using System;
using System.Reflection;
using Firebase.Auth;
using Firebase.Extensions;
using HutongGames.PlayMaker;
using UnityEngine;

namespace CustomPlaymakerActions
{
    [ActionCategory("Firebase")]
    [HutongGames.PlayMaker.Tooltip("Register a new user with email, password and username. Works with different Firebase Unity SDK return types.")]
    public class FirebaseRegisterUser : FsmStateAction
    {
        [RequiredField] public FsmString email;
        [RequiredField] public FsmString password;
        [RequiredField] public FsmString username;

        [UIHint(UIHint.FsmEvent)] public FsmEvent onRegisterSuccess;
        [UIHint(UIHint.FsmEvent)] public FsmEvent onRegisterFailed;

        [UIHint(UIHint.FsmString)] public FsmString userId;
        [UIHint(UIHint.FsmString)] public FsmString errorMessage;

        public override void Reset()
        {
            email = null;
            password = null;
            username = null;
            onRegisterSuccess = null;
            onRegisterFailed = null;
            userId = null;
            errorMessage = null;
        }

        public override void OnEnter()
        {
            try
            {
                var auth = FirebaseAuth.DefaultInstance;
                var createTask = auth.CreateUserWithEmailAndPasswordAsync(email.Value, password.Value);

                createTask.ContinueWithOnMainThread(task =>
                {
                    try
                    {
                        if (IsCanceledOrFaulted(task, out string failMsg))
                        {
                            errorMessage.Value = failMsg;
                            Fsm.Event(onRegisterFailed);
                            Finish();
                            return;
                        }

                        // Use reflection to get the Result object (works with Task<T> and older Task<AuthResult>)
                        object resultObj = null;
                        PropertyInfo resultProp = task.GetType().GetProperty("Result");
                        if (resultProp != null)
                            resultObj = resultProp.GetValue(task);

                        if (resultObj == null)
                        {
                            // No Result property (very old SDK) — fail gracefully
                            errorMessage.Value = "Registration succeeded but result unavailable";
                            Fsm.Event(onRegisterFailed);
                            Finish();
                            return;
                        }

                        // Try to obtain FirebaseUser:
                        FirebaseUser firebaseUser = resultObj as FirebaseUser;
                        if (firebaseUser == null)
                        {
                            // Older SDK: AuthResult contains a 'User' property
                            PropertyInfo userProp = resultObj.GetType().GetProperty("User");
                            if (userProp != null)
                                firebaseUser = userProp.GetValue(resultObj) as FirebaseUser;
                        }

                        if (firebaseUser == null)
                        {
                            errorMessage.Value = "Unable to extract FirebaseUser from auth result";
                            Fsm.Event(onRegisterFailed);
                            Finish();
                            return;
                        }

                        // Populate userId using available property
                        string id = firebaseUser.UserId;
                        if (string.IsNullOrEmpty(id))
                        {
                            // older property name fallback via reflection
                            PropertyInfo userIdProp = firebaseUser.GetType().GetProperty("UserId") ?? firebaseUser.GetType().GetProperty("Uid");
                            if (userIdProp != null)
                                id = userIdProp.GetValue(firebaseUser)?.ToString();
                        }

                        userId.Value = id ?? string.Empty;

                        // Update display name (username)
                        var profile = new UserProfile { DisplayName = username.Value };
                        var updateTask = firebaseUser.UpdateUserProfileAsync(profile);
                        updateTask.ContinueWithOnMainThread(t2 =>
                        {
                            if (IsCanceledOrFaulted(t2, out string updateFail))
                            {
                                errorMessage.Value = updateFail;
                                Fsm.Event(onRegisterFailed);
                            }
                            else
                            {
                                Fsm.Event(onRegisterSuccess);
                            }
                            Finish();
                        });
                    }
                    catch (Exception ex)
                    {
                        errorMessage.Value = ex.GetBaseException().Message;
                        Fsm.Event(onRegisterFailed);
                        Finish();
                    }
                });
            }
            catch (Exception e)
            {
                errorMessage.Value = e.GetBaseException().Message;
                Fsm.Event(onRegisterFailed);
                Finish();
            }
        }

        static bool IsCanceledOrFaulted(System.Threading.Tasks.Task task, out string message)
        {
            message = string.Empty;
            if (task.IsCanceled)
            {
                message = "Operation canceled";
                return true;
            }
            if (task.IsFaulted)
            {
                var ex = task.Exception?.GetBaseException();
                message = ex != null ? ex.Message : "Operation faulted";
                return true;
            }
            return false;
        }
    }
}
using System;
using Firebase.Auth;
using Firebase.Extensions;
using HutongGames.PlayMaker;
using UnityEngine;

namespace CustomPlaymakerActions
{
    [ActionCategory("Firebase")]
    [HutongGames.PlayMaker.Tooltip("Sign in a user with email and password using Firebase Auth.")]
    public class FirebaseLoginUser : FsmStateAction
    {
        [RequiredField] public FsmString email;
        [RequiredField] public FsmString password;

        [UIHint(UIHint.FsmEvent)] public FsmEvent onLoginSuccess;
        [UIHint(UIHint.FsmEvent)] public FsmEvent onLoginFailed;

        [UIHint(UIHint.FsmString)] [HutongGames.PlayMaker.Tooltip("Outputs signed-in user's UID")] public FsmString userId;
        [UIHint(UIHint.FsmString)] [HutongGames.PlayMaker.Tooltip("Outputs error message on failure")] public FsmString errorMessage;

        public override void Reset()
        {
            email = null;
            password = null;
            onLoginSuccess = null;
            onLoginFailed = null;
            userId = null;
            errorMessage = null;
        }

        public override void OnEnter()
        {
            try
            {
                string e = email.Value?.Trim();
                string p = password.Value?.Trim();

                if (string.IsNullOrEmpty(e) || string.IsNullOrEmpty(p))
                {
                    errorMessage.Value = "Email and password are required";
                    Fsm.Event(onLoginFailed);
                    Finish();
                    return;
                }

                var auth = FirebaseAuth.DefaultInstance;
                var signInTask = auth.SignInWithEmailAndPasswordAsync(e, p);

                signInTask.ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        errorMessage.Value = "Login canceled";
                        Fsm.Event(onLoginFailed);
                        Finish();
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        var baseEx = task.Exception?.GetBaseException();
                        // Try to surface Firebase-specific message if present
                        string msg = baseEx != null ? baseEx.Message : "Login failed";
                        errorMessage.Value = msg;
                        Fsm.Event(onLoginFailed);
                        Finish();
                        return;
                    }

                    // Compatible with SDKs that return Task<FirebaseUser> or Task<AuthResult>
                    object resultObj = null;
                    var resultProp = task.GetType().GetProperty("Result");
                    if (resultProp != null) resultObj = resultProp.GetValue(task);

                    FirebaseUser user = null;
                    if (resultObj is FirebaseUser fu) user = fu;
                    else if (resultObj != null)
                    {
                        // older SDK: AuthResult has 'User' property
                        var userProp = resultObj.GetType().GetProperty("User");
                        if (userProp != null) user = userProp.GetValue(resultObj) as FirebaseUser;
                    }

                    if (user == null)
                    {
                        // fallback to FirebaseAuth.CurrentUser
                        user = FirebaseAuth.DefaultInstance.CurrentUser;
                    }

                    if (user == null)
                    {
                        errorMessage.Value = "Login succeeded but user not available";
                        Fsm.Event(onLoginFailed);
                        Finish();
                        return;
                    }

                    // output UID
                    userId.Value = user.UserId ?? string.Empty;
                    Fsm.Event(onLoginSuccess);
                    Finish();
                });
            }
            catch (Exception ex)
            {
                errorMessage.Value = ex.GetBaseException().Message;
                Fsm.Event(onLoginFailed);
                Finish();
            }
        }
    }
}
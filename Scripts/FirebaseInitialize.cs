using Firebase;
using Firebase.Extensions;
using HutongGames.PlayMaker;
using UnityEngine;

namespace CustomPlaymakerActions
{
    [ActionCategory("Firebase")]
    [HutongGames.PlayMaker.Tooltip("Initialize Firebase and check dependencies.")]
    public class FirebaseInitialize : FsmStateAction
    {
        [UIHint(UIHint.FsmEvent)]
        public FsmEvent onSuccess;
        
        [UIHint(UIHint.FsmEvent)]
        public FsmEvent onFailure;

        public override void Reset()
        {
            onSuccess = null;
            onFailure = null;
        }

        public override void OnEnter()
        {
            FirebaseApp.CheckAndFixDependenciesAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.Result == DependencyStatus.Available)
                        Fsm.Event(onSuccess);
                    else
                        Fsm.Event(onFailure);

                    Finish();
                });
        }
    }
}
using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NGA {

    public class FGContractEvent {
        public string EventKey { get; set; }
        public class OnSosigKillEvent {
            public Sosig Sosig { get; set; }
        }
        public class OnSosigMadeEnemyWithEvent {
            public Sosig Sosig { get; set; }
            public int IFF { get; set; }
        }
        public class OnSosigAlertEvent {
            public Sosig Sosig { get; set; }
            public Vector3 Position { get; set; }
        }
        public class OnShotFiredEvent {
            public FVRFireArm Firearm { get; set; }
        }
        public class OnSosiggunFiredEvent {
            public SosigWeapon Weapon { get; set; }
        }
        public OnSosigKillEvent OnSosigKill { get; set; }
        public OnSosigMadeEnemyWithEvent OnSosigMadeEnemyWith { get; set; }
        public OnSosigAlertEvent OnSosigAlert { get; set; }
        public OnShotFiredEvent OnShotFired { get; set; }
        public OnSosiggunFiredEvent OnSosiggunFired { get; set; }
        // For use with custom events where modder may serialize custom class.
        public string GenericEventContents { get; set; }
    }

    public class FGContractEventsRecorder {
        // Generic event multiplexer for receiving classic H3 events and those added by modders.
        public Action<FGContractEvent> OnEventHappened { get; set; }
        // Event multiplexer for notifying listeners that an event has been registered.
        public Action<FGContractEvent> OnEventRegistered { get; set; }
        public List<FGContractEvent> CurrentSessionEvents { get; private set; } = new List<FGContractEvent>();
        private bool sessionActive = false;
        public FGContractEventsRecorder() {
            OnEventHappened += AppendEventToSession;
        }

        public void AppendEventToSession(FGContractEvent contractEvent) {
            if (!sessionActive) {
                return;
            }
            CurrentSessionEvents.Add(contractEvent);
            OnEventRegistered?.Invoke(contractEvent);
        }
        public void StartSession() {
            sessionActive = true;
            CurrentSessionEvents.Clear();
            ListenToClassicH3Events();
        }
        public void WipeSession() {
            sessionActive = false;
            CurrentSessionEvents.Clear();
            StopListeningToClassicH3Events();
        }

        private void ListenToClassicH3Events() {
            GM.CurrentSceneSettings.SosigKillEvent += this.OnSosigKill;
			GM.CurrentSceneSettings.SosigMadeEnemyWithEvent += this.OnSosigMadeEnemyWith;
			GM.CurrentSceneSettings.SosigAlertEvent += this.OnSosigAlert;
			GM.CurrentSceneSettings.ShotFiredEvent += this.OnShotFired;
			GM.CurrentSceneSettings.SosiggunFiredEvent += this.OnSosiggunFired;
			GM.CurrentSceneSettings.SosigFleeFromEvent += this.OnSosigFleeFrom;
        }
        private void OnSosigKill(Sosig s)
		{
			FGContractEvent contractEvent = new FGContractEvent();
            contractEvent.EventKey = "OnSosigKill";
            contractEvent.OnSosigKill = new FGContractEvent.OnSosigKillEvent { Sosig = s };
            OnEventHappened?.Invoke(contractEvent);
		}
        private void OnSosigMadeEnemyWith(Sosig S, int iff)
		{
			FGContractEvent contractEvent = new FGContractEvent();
            contractEvent.EventKey = "OnSosigMadeEnemyWith";
            contractEvent.OnSosigMadeEnemyWith = new FGContractEvent.OnSosigMadeEnemyWithEvent { Sosig = S, IFF = iff };
            OnEventHappened?.Invoke(contractEvent);
		}
		private void OnSosigAlert(Sosig s, Vector3 p)
		{
			FGContractEvent contractEvent = new FGContractEvent();
            contractEvent.EventKey = "OnSosigAlert";
            contractEvent.OnSosigAlert = new FGContractEvent.OnSosigAlertEvent { Sosig = s, Position = p };
            OnEventHappened?.Invoke(contractEvent);
		}
		private void OnShotFired(FVRFireArm firearm)
		{
			FGContractEvent contractEvent = new FGContractEvent();
            contractEvent.EventKey = "OnShotFired";
            contractEvent.OnShotFired = new FGContractEvent.OnShotFiredEvent { Firearm = firearm };
            OnEventHappened?.Invoke(contractEvent);
		}
		private void OnSosiggunFired(SosigWeapon weapon)
		{
			FGContractEvent contractEvent = new FGContractEvent();
            contractEvent.EventKey = "OnSosiggunFired";
            contractEvent.OnSosiggunFired = new FGContractEvent.OnSosiggunFiredEvent { Weapon = weapon };
            OnEventHappened?.Invoke(contractEvent);
		}
		private void OnSosigFleeFrom(Sosig S, int iff)
		{
			FGContractEvent contractEvent = new FGContractEvent();
            contractEvent.EventKey = "OnSosigFleeFrom";
            contractEvent.OnSosigMadeEnemyWith = new FGContractEvent.OnSosigMadeEnemyWithEvent { Sosig = S, IFF = iff };
            OnEventHappened?.Invoke(contractEvent);
		}

        private void StopListeningToClassicH3Events() {
            GM.CurrentSceneSettings.SosigKillEvent -= this.OnSosigKill;
			GM.CurrentSceneSettings.SosigMadeEnemyWithEvent -= this.OnSosigMadeEnemyWith;
			GM.CurrentSceneSettings.SosigAlertEvent -= this.OnSosigAlert;
			GM.CurrentSceneSettings.ShotFiredEvent -= this.OnShotFired;
			GM.CurrentSceneSettings.SosiggunFiredEvent -= this.OnSosiggunFired;
			GM.CurrentSceneSettings.SosigFleeFromEvent -= this.OnSosigFleeFrom;
        }
    }

} // namespace NGA
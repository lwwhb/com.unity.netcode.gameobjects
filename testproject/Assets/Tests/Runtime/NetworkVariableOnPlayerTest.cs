using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;

namespace TestProject.RuntimeTests
{
    public class PlayerNetworkVariableTest : NetworkBehaviour
    {
        public static PlayerNetworkVariableTest ServerInstance;
        public static PlayerNetworkVariableTest ClientInstance;
        public NetworkVariable<int> SomeNetworkVariable = new NetworkVariable<int>();
        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                if (IsServer)
                {
                    ServerInstance = this;
                }
                else
                {
                    Debug.Log($"Client-{NetworkManager.LocalClientId} spawned local player object and registered to receive NetworkVariable notifications.");
                    ClientInstance = this;
                    SomeNetworkVariable.OnValueChanged = LocalPlayerSomeNetworkVariableUpdated;
                }
            }
            else if (NetworkObject.IsPlayerObject && !IsOwner)
            {
                Debug.Log($"[Non-Local] Client-{OwnerClientId} player object spawned and registered to receive NetworkVariable notifications.");
                SomeNetworkVariable.OnValueChanged = NonLocalPlayerInstanceNetworkVariableUpdated;
            }
        }

        private void LocalPlayerSomeNetworkVariableUpdated(int previous, int current)
        {
            Debug.Log($"Client-{NetworkManager.LocalClientId} received update {current}");
        }

        private void NonLocalPlayerInstanceNetworkVariableUpdated(int previous, int current)
        {
            Debug.Log($"[Non-Local] Client-{OwnerClientId} received update {current}");
        }
    }

    public class NetworkVariableOnPlayerTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<PlayerNetworkVariableTest>();
            base.OnCreatePlayerPrefab();
        }

        private bool WaitForLocalClientToUpdateValue()
        {
            if(PlayerNetworkVariableTest.ServerInstance.SomeNetworkVariable.Value != PlayerNetworkVariableTest.ClientInstance.SomeNetworkVariable.Value)
            {
                return false;
            }
            return true;
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableOnPlayer()
        {
            PlayerNetworkVariableTest.ServerInstance.SomeNetworkVariable.Value++;
            yield return WaitForConditionOrTimeOut(WaitForLocalClientToUpdateValue);
            AssertOnTimeout($"Client NetworkVariable value: {PlayerNetworkVariableTest.ClientInstance.SomeNetworkVariable.Value} never was updated to server value: {PlayerNetworkVariableTest.ServerInstance.SomeNetworkVariable.Value}");
        }
    }
}
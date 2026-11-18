using Unity.Netcode;

namespace Assets._Scripts._Core.Network
{
    public abstract class NetworkBehaviourBase : NetworkBehaviour
    {
        public sealed override void OnNetworkSpawn()
        {
            NetworkDiResolver.Inject(gameObject);
            OnSpawned();
        }

        protected virtual void OnSpawned() { }
    }
}

using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets._Scripts._Core
{
    public static partial class NetworkDiResolver
    {
        [AutoStaticsCleanup] private static IObjectResolver _resolver;


        public static void Initialize(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public static T Resolve<T>() where T : class
        {
            return _resolver != null ? _resolver.Resolve<T>() : null;
        }

        public static void Inject(GameObject target)
        {
            if (_resolver != null && target != null)
                _resolver.InjectGameObject(target); 
        }
    }
}
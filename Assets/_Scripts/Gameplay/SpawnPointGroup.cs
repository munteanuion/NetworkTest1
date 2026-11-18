using UnityEngine;

namespace Assets._Scripts.Gameplay
{
    public sealed class SpawnPointGroup : MonoBehaviour
    {
        [SerializeField] private Transform[] _points;
        private int _index;


        public Pose Next()
        {
            if (_points == null || _points.Length == 0)
                return new Pose(new Vector3(0, 1.2f, 0), Quaternion.identity);

            var point = _points[_index++ % _points.Length];
            return new Pose(point.position, point.rotation);
        }
    }
}

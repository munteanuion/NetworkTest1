using UnityEngine;
using UnityEngine.UI;

namespace Assets._Scripts.UI
{
    public sealed class LoadingView : MonoBehaviour
    {
        [SerializeField] private Text _label;

        public void Show(string text)
        {
            if (_label != null)
                _label.text = text;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
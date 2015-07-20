using UnityEngine;
using UnityEngine.UI;

namespace SampleLibrary
{
    [RequireComponent(typeof(Text))]
    public class TextBrightnessBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 1フレームあたりどのくらい輝度が変わるか。
        /// 輝度の範囲は[0, 1]
        /// </summary>
        [SerializeField]
        float _speed = 0;

        private Text _text;
        private float _brightness;
        private bool _increase;

        void Start()
        {
            _text = GetComponent<Text>();
        }

        void Update()
        {
            if (_increase)
            {
                _brightness += _speed;
                if (_brightness > 1)
                {
                    _brightness = 1;
                    _increase = false;
                }
            }
            else
            {
                _brightness -= _speed;
                if (_brightness < 0)
                {
                    _brightness = 0;
                    _increase = transform;
                }
            }

            _text.color = new Color(_brightness, _brightness, _brightness, 1);
        }
    }
}

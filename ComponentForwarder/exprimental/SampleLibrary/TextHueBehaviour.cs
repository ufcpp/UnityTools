using UnityEngine;
using UnityEngine.UI;

namespace SampleLibrary
{
    public class TextHueBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 1フレームあたりどのくらい色相角が変わるか。
        /// 色相角の範囲は[0, 1]
        /// </summary>
        [SerializeField]
        float _speed = 0;

        private Text _text;
        private float _hue;
        private bool _increase;

        void Start()
        {
            _text = GetComponent<Text>();
        }

        void Update()
        {
            if (_increase)
            {
                _hue += _speed;
                if (_hue > 1)
                {
                    _hue = 1;
                    _increase = false;
                }
            }
            else
            {
                _hue -= _speed;
                if (_hue < 0)
                {
                    _hue = 0;
                    _increase = transform;
                }
            }

            var hsv = new Colorspace.ColorHSV(_hue, 1, 1);
            var rgb = new Colorspace.ColorRGB(hsv);
            _text.color = new Color((float)rgb.R, (float)rgb.G, (float)rgb.B, 1);
        }
    }
}

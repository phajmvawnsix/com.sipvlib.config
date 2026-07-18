#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

namespace SiPVLib.Config.Configs
{
    public class SpriteConfig : GameConfig
    {
        [SerializeField] protected bool _isSingle, _randomSprite;
#if ODIN_INSPECTOR
        [SerializeField, ShowIf(nameof(_isSingle))] protected Sprite _sprite;
        [SerializeField, HideIf(nameof(_isSingle))] protected Sprite[] _sprites;
#else
        [SerializeField] protected Sprite _sprite;
        [SerializeField] protected Sprite[] _sprites;
#endif
        
        public Sprite Sprite => GetSprite();
        public Sprite[] Sprites => _sprites;
        
        public Sprite GetSprite()
        {
            if (_isSingle) return _sprite;
            if (_sprites == null || _sprites.Length == 0) return null;
            return _randomSprite ? _sprites[Random.Range(0, _sprites.Length)] : _sprites[0];
        }
    }
}
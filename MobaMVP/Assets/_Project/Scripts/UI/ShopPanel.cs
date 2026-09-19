using UnityEngine;
using UnityEngine.UI;
using Moba.Combat;
using Moba.Core;

namespace Moba.UI
{
    /// <summary>In-match shop: press B to toggle, buy one of six items.</summary>
    public sealed class ShopPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button[] _buyButtons;
        [SerializeField] private Text[] _itemLabels;

        private HeroCombat _local;
        private bool _visible;

        private void Awake()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void OnEnable()
        {
            WireButtons();
        }

        private void Update()
        {
            if (_local == null)
            {
                TryBind();
            }

            if (!_visible || _statusText == null || _local == null)
            {
                return;
            }

            _statusText.text = $"金币 {_local.Gold}  |  背包 {_local.GetItemSlot(0)}/{_local.GetItemSlot(1)}/...";
        }

        public void Toggle()
        {
            _visible = !_visible;
            if (_root != null)
            {
                _root.SetActive(_visible);
            }

            RefreshLabels();
        }

        private void TryBind()
        {
            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (combat.IsOwner && combat.Kind == CombatTargetKind.Hero)
                {
                    _local = combat;
                    return;
                }
            }
        }

        private void WireButtons()
        {
            if (_buyButtons == null)
            {
                return;
            }

            for (var i = 0; i < _buyButtons.Length && i < ItemCatalog.All.Length; i++)
            {
                var idx = i;
                var btn = _buyButtons[i];
                if (btn == null)
                {
                    continue;
                }

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnBuy(idx));
            }
        }

        private void RefreshLabels()
        {
            if (_titleText != null)
            {
                _titleText.text = "装备商店 (B 关闭)";
            }

            if (_itemLabels == null)
            {
                return;
            }

            for (var i = 0; i < _itemLabels.Length && i < ItemCatalog.All.Length; i++)
            {
                if (_itemLabels[i] == null)
                {
                    continue;
                }

                var item = ItemCatalog.All[i];
                _itemLabels[i].text = $"{item.DisplayName}  {item.Cost}金";
            }
        }

        private void OnBuy(int index)
        {
            if (_local == null)
            {
                TryBind();
            }

            if (_local == null || index < 0 || index >= ItemCatalog.All.Length)
            {
                return;
            }

            _local.RequestBuyItem(ItemCatalog.All[index].Id);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Moba.Combat;
using Moba.Core;

namespace Moba.UI
{
    /// <summary>Simple top-down lane minimap: heroes, towers, crystals as dots.</summary>
    public sealed class MinimapView : MonoBehaviour
    {
        [SerializeField] private RectTransform _mapRoot;
        [SerializeField] private float _worldMinX = -18f;
        [SerializeField] private float _worldMaxX = 18f;
        [SerializeField] private float _worldMinZ = -50f;
        [SerializeField] private float _worldMaxZ = 50f;

        private readonly List<Image> _dots = new();
        private readonly List<RectTransform> _dotRects = new();

        private void LateUpdate()
        {
            if (_mapRoot == null)
            {
                return;
            }

            var units = CollectUnits();
            EnsureDotCount(units.Count);
            for (var i = 0; i < units.Count; i++)
            {
                var u = units[i];
                _dotRects[i].anchoredPosition = WorldToMap(u.Position);
                _dots[i].color = u.Color;
                _dotRects[i].sizeDelta = u.Size;
                _dots[i].enabled = true;
            }

            for (var i = units.Count; i < _dots.Count; i++)
            {
                _dots[i].enabled = false;
            }
        }

        private struct MapUnit
        {
            public Vector3 Position;
            public Color Color;
            public Vector2 Size;
        }

        private List<MapUnit> CollectUnits()
        {
            var list = new List<MapUnit>(16);
            foreach (var hero in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (hero.Kind != CombatTargetKind.Hero || !hero.IsAlive)
                {
                    continue;
                }

                list.Add(new MapUnit
                {
                    Position = hero.WorldPosition,
                    Color = TeamColor(hero.Team, hero.IsOwner && !hero.IsBotUnit),
                    Size = hero.IsOwner && !hero.IsBotUnit ? new Vector2(14f, 14f) : new Vector2(10f, 10f)
                });
            }

            foreach (var tower in FindObjectsByType<DefenseTower>(FindObjectsSortMode.None))
            {
                if (!tower.IsAttackable)
                {
                    continue;
                }

                list.Add(new MapUnit
                {
                    Position = tower.WorldPosition,
                    Color = TeamColor(tower.Team, false) * 0.85f,
                    Size = new Vector2(12f, 12f)
                });
            }

            foreach (var crystal in FindObjectsByType<NexusCrystal>(FindObjectsSortMode.None))
            {
                if (!crystal.IsAlive)
                {
                    continue;
                }

                list.Add(new MapUnit
                {
                    Position = crystal.WorldPosition,
                    Color = Color.Lerp(TeamColor(crystal.Team, false), Color.white, 0.35f),
                    Size = new Vector2(16f, 16f)
                });
            }

            return list;
        }

        private static Color TeamColor(TeamId team, bool self)
        {
            if (self)
            {
                return new Color(1f, 0.92f, 0.2f);
            }

            return team switch
            {
                TeamId.Blue => new Color(0.25f, 0.55f, 1f),
                TeamId.Red => new Color(1f, 0.35f, 0.3f),
                _ => Color.gray
            };
        }

        private Vector2 WorldToMap(Vector3 world)
        {
            var nx = Mathf.InverseLerp(_worldMinX, _worldMaxX, world.x);
            var nz = Mathf.InverseLerp(_worldMinZ, _worldMaxZ, world.z);
            var rt = _mapRoot.rect;
            return new Vector2(
                Mathf.Lerp(rt.xMin, rt.xMax, nx),
                Mathf.Lerp(rt.yMin, rt.yMax, nz));
        }

        private void EnsureDotCount(int count)
        {
            while (_dots.Count < count)
            {
                var go = new GameObject($"Dot_{_dots.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(_mapRoot, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(10f, 10f);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                _dots.Add(img);
                _dotRects.Add(rt);
            }
        }
    }
}

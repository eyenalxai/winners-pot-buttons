using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BlackJacket.WinnersPotButtons
{
    internal enum HudContext
    {
        None,
        Match,
        Shop,
        Campaign
    }

    /// <summary>
    /// Runtime +/- buttons for the winners pot and for the player's own coins.
    /// The same grid is shown next to whichever counter is visible: the table HUD
    /// during a match, the shop HUD while shopping, or the campaign HUD on the map.
    /// </summary>
    internal sealed class WinnersPotButtonsOverlay : MonoBehaviour
    {
        private enum Cluster
        {
            WinnersPot,
            Player
        }

        private static readonly Color MinusColor = new Color(0.68f, 0.27f, 0.27f, 0.92f);
        private static readonly Color MinColor = new Color(0.48f, 0.16f, 0.16f, 0.92f);
        private static readonly Color PlusColor = new Color(0.24f, 0.58f, 0.31f, 0.92f);
        private static readonly Color MaxColor = new Color(0.20f, 0.42f, 0.63f, 0.92f);

        private const int Columns = 4;
        private const int ButtonCount = 8;

        private Canvas _canvas;
        private RectTransform _canvasRt;
        private RectTransform _potRow;
        private RectTransform _playerRow;
        private bool _errorLogged;

        private PlayerUI _cachedPlayerUI;
        private Transform _matchPotAnchor;
        private Transform _matchPlayerAnchor;
        private Transform _shopPotAnchor;
        private Transform _shopPlayerAnchor;
        private Transform _campaignPotAnchor;
        private Transform _campaignPlayerAnchor;

        private float _layoutSize = -1f;
        private float _layoutSpacing = -1f;
        private float _layoutFont = -1f;

        private void Update()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    WinnersPotButtonsPlugin.Log?.LogWarning($"WinnersPotButtons error (further errors suppressed): {e}");
                }
                HideAll();
            }
        }

        private void Tick()
        {
            var cfg = WinnersPotButtonsPlugin.Cfg;
            var gc = GameController.Instance;

            if (cfg == null || !cfg.Enabled.Value || gc == null || gc.UI == null || gc.CampaignState == null)
            {
                HideAll();
                return;
            }

            var context = DetectContext();
            if (context == HudContext.None)
            {
                HideAll();
                return;
            }

            var playerUI = CampaignUI.Instance != null ? CampaignUI.Instance.PlayerUI : null;
            if (playerUI == null)
            {
                HideAll();
                return;
            }

            ResolveAnchors(gc, playerUI);
            EnsureUi(gc);
            ApplyLayout(cfg);

            var potAnchor = SelectAnchor(context, Cluster.WinnersPot, gc, playerUI);
            var playerAnchor = SelectAnchor(context, Cluster.Player, gc, playerUI);

            bool showPot = cfg.ShowWinnersPotButtons.Value && IsUsable(potAnchor);
            bool showPlayer = cfg.ShowPlayerButtons.Value && IsUsable(playerAnchor);

            SetVisible(_potRow, showPot);
            SetVisible(_playerRow, showPlayer);

            if (showPot)
            {
                PositionCluster(cfg, _potRow, potAnchor);
            }
            if (showPlayer)
            {
                PositionCluster(cfg, _playerRow, playerAnchor);
            }
        }

        private void HideAll()
        {
            SetVisible(_potRow, false);
            SetVisible(_playerRow, false);
        }

        // ------------------------------------------------------------------ context

        private static HudContext DetectContext()
        {
            var config = GameController.Config;
            if (config != null)
            {
                if (config.PhaseShop != null && config.PhaseShop.IsActive)
                {
                    return HudContext.Shop;
                }
                if (config.PhaseCampaignMap != null && config.PhaseCampaignMap.IsActive)
                {
                    return HudContext.Campaign;
                }
            }

            // Fallbacks in case the phase assets are unavailable.
            var shop = Shop.Instance;
            if (shop != null && shop.gameObject.activeSelf)
            {
                return HudContext.Shop;
            }
            var campaign = CampaignUI.Instance;
            if (campaign != null && campaign.IsActive)
            {
                return HudContext.Campaign;
            }

            return HudContext.Match;
        }

        // ------------------------------------------------------------------ anchors

        private void ResolveAnchors(GameController gc, PlayerUI playerUI)
        {
            if (_cachedPlayerUI == playerUI && _matchPotAnchor != null)
            {
                return;
            }

            _cachedPlayerUI = playerUI;
            _matchPotAnchor = null;
            _matchPlayerAnchor = null;
            _shopPotAnchor = null;
            _shopPlayerAnchor = null;
            _campaignPotAnchor = null;
            _campaignPlayerAnchor = null;

            Transform campaignGroup = playerUI.transform.parent; // PlayerStats_Campaign
            Transform groupsRoot = campaignGroup != null ? campaignGroup.parent : null;
            Transform matchGroup = groupsRoot != null ? groupsRoot.Find("PlayerStats_Match") : null;
            Transform shopGroup = groupsRoot != null ? groupsRoot.Find("PlayerStats_Shop") : null;

            if (matchGroup != null)
            {
                _matchPotAnchor = matchGroup.Find("HideOnShowMap/Stat_WinnersPot");
                _matchPlayerAnchor = matchGroup.Find("HideOnShowMap/Stat_Stash");
            }
            if (shopGroup != null)
            {
                _shopPotAnchor = shopGroup.Find("HideOnShowMap/Stat_WinnersPot");
                _shopPlayerAnchor = shopGroup.Find("HideOnShowMap/Stat_Stash");
            }
            if (campaignGroup != null)
            {
                _campaignPotAnchor = campaignGroup.Find("PlayerUI/LeftCornerHolder/Stat_Winners_Pot_Coins");
                _campaignPlayerAnchor = campaignGroup.Find("PlayerUI/LeftCornerHolder/Stat_Coins");
            }

            // Fallbacks to the references the game exposes directly.
            if (_matchPotAnchor == null && gc.UI.WinnersPotCountText != null)
            {
                _matchPotAnchor = gc.UI.WinnersPotCountText.transform;
            }
            if (_matchPlayerAnchor == null && gc.UI.Player != null && gc.UI.Player.StashCountText != null)
            {
                _matchPlayerAnchor = gc.UI.Player.StashCountText.transform;
            }
            if (_shopPotAnchor == null)
            {
                _shopPotAnchor = _matchPotAnchor;
            }
            if (_shopPlayerAnchor == null)
            {
                _shopPlayerAnchor = _matchPlayerAnchor;
            }
        }

        private Transform SelectAnchor(HudContext context, Cluster cluster, GameController gc, PlayerUI playerUI)
        {
            Transform anchor = null;
            switch (context)
            {
                case HudContext.Match:
                    anchor = cluster == Cluster.WinnersPot ? _matchPotAnchor : _matchPlayerAnchor;
                    break;
                case HudContext.Shop:
                    anchor = cluster == Cluster.WinnersPot ? _shopPotAnchor : _shopPlayerAnchor;
                    break;
                case HudContext.Campaign:
                    anchor = cluster == Cluster.WinnersPot ? _campaignPotAnchor : _campaignPlayerAnchor;
                    break;
            }

            if (anchor == null && playerUI != null)
            {
                var text = cluster == Cluster.WinnersPot ? playerUI.WinnersPotCount : playerUI.CoinCount;
                if (text != null)
                {
                    anchor = text.transform;
                }
            }

            return anchor;
        }

        private static bool IsUsable(Transform anchor)
        {
            return anchor != null && anchor.gameObject.activeInHierarchy;
        }

        // ------------------------------------------------------------------ ui

        private void EnsureUi(GameController gc)
        {
            if (_canvas == null)
            {
                var canvasGo = new GameObject("WinnersPotButtonsCanvas");
                DontDestroyOnLoad(canvasGo);
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 31000;
                canvasGo.AddComponent<GraphicRaycaster>();
                _canvasRt = (RectTransform)canvasGo.transform;
            }

            if (_potRow == null)
            {
                _potRow = CreateCluster(gc, "WinnersPotButtons", Cluster.WinnersPot);
            }
            if (_playerRow == null)
            {
                _playerRow = CreateCluster(gc, "PlayerCoinsButtons", Cluster.Player);
            }
        }

        private RectTransform CreateCluster(GameController gc, string name, Cluster cluster)
        {
            var rowGo = new GameObject(name, typeof(RectTransform));
            rowGo.transform.SetParent(_canvas.transform, false);
            var row = (RectTransform)rowGo.transform;
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);

            TMP_FontAsset font = ResolveFont(gc);

            AddButton(row, "MIN", MinColor, font, () => OnMin(cluster));
            AddButton(row, "-10", MinusColor, font, () => OnRemove(cluster, 10));
            AddButton(row, "-5", MinusColor, font, () => OnRemove(cluster, 5));
            AddButton(row, "-1", MinusColor, font, () => OnRemove(cluster, 1));
            AddButton(row, "+1", PlusColor, font, () => OnAdd(cluster, 1));
            AddButton(row, "+5", PlusColor, font, () => OnAdd(cluster, 5));
            AddButton(row, "+10", PlusColor, font, () => OnAdd(cluster, 10));
            AddButton(row, "MAX", MaxColor, font, () => OnMax(cluster));

            return row;
        }

        private static TMP_FontAsset ResolveFont(GameController gc)
        {
            TextMeshProUGUI sample = null;

            if (gc.UI.WinnersPotCountText != null)
            {
                sample = gc.UI.WinnersPotCountText.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (sample == null && gc.UI.Player != null && gc.UI.Player.StashCountText != null)
            {
                sample = gc.UI.Player.StashCountText.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (sample == null && gc.UI.TableValueDisplay != null && gc.UI.TableValueDisplay.PlayerCounter != null)
            {
                sample = gc.UI.TableValueDisplay.PlayerCounter.CountText;
            }
            if (sample == null && CampaignUI.Instance != null && CampaignUI.Instance.PlayerUI != null)
            {
                sample = CampaignUI.Instance.PlayerUI.CoinCount;
            }

            return sample != null ? sample.font : null;
        }

        private static void AddButton(RectTransform row, string label, Color color, TMP_FontAsset font, UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(row, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private void ApplyLayout(Settings cfg)
        {
            float size = Mathf.Max(16f, cfg.ButtonSize.Value);
            float spacing = Mathf.Max(0f, cfg.Spacing.Value);
            float font = Mathf.Max(8f, cfg.FontSize.Value);

            if (Mathf.Approximately(size, _layoutSize) && Mathf.Approximately(spacing, _layoutSpacing)
                && Mathf.Approximately(font, _layoutFont))
            {
                return;
            }

            _layoutSize = size;
            _layoutSpacing = spacing;
            _layoutFont = font;

            LayoutCluster(_potRow, size, spacing, font);
            LayoutCluster(_playerRow, size, spacing, font);
        }

        private static void LayoutCluster(RectTransform row, float size, float spacing, float font)
        {
            if (row == null)
            {
                return;
            }

            row.sizeDelta = new Vector2(Columns * size + (Columns - 1) * spacing, 2f * size + spacing);

            for (int i = 0; i < row.childCount && i < ButtonCount; i++)
            {
                var child = (RectTransform)row.GetChild(i);
                int column = i % Columns;
                int line = i / Columns;

                child.sizeDelta = new Vector2(size, size);
                child.anchoredPosition = new Vector2(
                    (column - (Columns - 1) * 0.5f) * (size + spacing),
                    (0.5f - line) * (size + spacing));

                var text = child.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null)
                {
                    text.fontSize = font;
                }
            }
        }

        private void PositionCluster(Settings cfg, RectTransform row, Transform anchor)
        {
            if (row == null || _canvasRt == null)
            {
                return;
            }

            var anchorRt = anchor as RectTransform;
            if (anchorRt == null)
            {
                return;
            }

            float width = row.sizeDelta.x;
            float height = row.sizeDelta.y;

            Camera cam = GetCanvasCamera(anchorRt);
            var corners = new Vector3[4];
            anchorRt.GetWorldCorners(corners);

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(cam, corners[3]);

            Vector2 center = (bottomLeft + topRight) * 0.5f;
            float topY = (topLeft.y + topRight.y) * 0.5f;
            float bottomY = (bottomLeft.y + bottomRight.y) * 0.5f;

            float margin = Mathf.Max(0f, cfg.Margin.Value);
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            float x = center.x + cfg.NudgeX.Value;
            float y = topY + margin + halfHeight + cfg.NudgeY.Value;

            // Prefer placing the grid above the counter; drop below when there is no room.
            if (y + halfHeight > Screen.height - 2f)
            {
                y = bottomY - margin - halfHeight + cfg.NudgeY.Value;
            }

            x = Mathf.Clamp(x, halfWidth + 2f, Screen.width - halfWidth - 2f);
            y = Mathf.Clamp(y, halfHeight + 2f, Screen.height - halfHeight - 2f);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, new Vector2(x, y), null, out Vector2 local))
            {
                row.anchoredPosition = local;
            }
        }

        private static Camera GetCanvasCamera(RectTransform rt)
        {
            var canvas = rt.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }
            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private static void SetVisible(RectTransform row, bool visible)
        {
            if (row != null && row.gameObject.activeSelf != visible)
            {
                row.gameObject.SetActive(visible);
            }
        }

        // ------------------------------------------------------------------ actions

        private void OnAdd(Cluster cluster, int amount)
        {
            var gc = GameController.Instance;
            if (gc == null || gc.CampaignState == null)
            {
                return;
            }

            if (DetectContext() == HudContext.Campaign)
            {
                if (cluster == Cluster.WinnersPot)
                {
                    var cfg = WinnersPotButtonsPlugin.Cfg;
                    if (cfg != null && cfg.IgnorePotCap.Value)
                    {
                        gc.CampaignState.SetWinnersPot(gc.CampaignState.WinnersPot + amount);
                    }
                    else
                    {
                        gc.CampaignState.AddCoinsToWinnersPot(amount);
                    }
                }
                else
                {
                    gc.CampaignState.AddCoins(amount);
                }
                return;
            }

            if (cluster == Cluster.WinnersPot)
            {
                var pot = gc.UI.WinnersPot;
                if (pot == null)
                {
                    return;
                }

                int liveCap = GetWinnersPotCap(gc);
                bool ignoreCap = WinnersPotButtonsPlugin.Cfg != null && WinnersPotButtonsPlugin.Cfg.IgnorePotCap.Value;
                int add = ignoreCap ? amount : Mathf.Min(amount, liveCap == -1 ? amount : Mathf.Max(liveCap - pot.CoinAmount, 0));
                if (add <= 0)
                {
                    return;
                }

                StartCoroutine(AddCoinsCo(cluster, pot, add, liveCap, ignoreCap));
            }
            else
            {
                var stash = GetPlayerZone(gc);
                if (stash == null)
                {
                    return;
                }

                int liveCap = GetPlayerCap(gc);
                int add = Mathf.Min(amount, liveCap == -1 ? amount : Mathf.Max(liveCap - stash.CoinAmount, 0));
                if (add <= 0)
                {
                    return;
                }

                StartCoroutine(AddCoinsCo(cluster, stash, add, liveCap, false));
            }
        }

        private void OnRemove(Cluster cluster, int amount)
        {
            var gc = GameController.Instance;
            if (gc == null || gc.CampaignState == null)
            {
                return;
            }

            if (DetectContext() == HudContext.Campaign)
            {
                if (cluster == Cluster.WinnersPot)
                {
                    gc.CampaignState.SetWinnersPot(Mathf.Max(gc.CampaignState.WinnersPot - amount, 0));
                }
                else
                {
                    gc.CampaignState.SetHealth(Mathf.Clamp(gc.CampaignState.PlayerHealth - amount, 0, gc.CampaignState.PlayerMaxHealth));
                }
                return;
            }

            var zone = cluster == Cluster.WinnersPot ? (CoinZone)gc.UI.WinnersPot : GetPlayerZone(gc);
            if (zone == null)
            {
                return;
            }

            RemoveCoinsFromZone(cluster, zone, amount);
        }

        private void OnMin(Cluster cluster)
        {
            var gc = GameController.Instance;
            if (gc == null || gc.CampaignState == null)
            {
                return;
            }

            if (DetectContext() == HudContext.Campaign)
            {
                if (cluster == Cluster.WinnersPot)
                {
                    gc.CampaignState.SetWinnersPot(0);
                }
                else
                {
                    gc.CampaignState.SetHealth(0);
                }
                return;
            }

            var zone = cluster == Cluster.WinnersPot ? (CoinZone)gc.UI.WinnersPot : GetPlayerZone(gc);
            if (zone == null)
            {
                return;
            }

            RemoveAllCoinsFromZone(cluster, zone);
        }

        private void OnMax(Cluster cluster)
        {
            var gc = GameController.Instance;
            if (gc == null || gc.CampaignState == null)
            {
                return;
            }

            bool campaignContext = DetectContext() == HudContext.Campaign;

            if (cluster == Cluster.WinnersPot)
            {
                int cap = GetWinnersPotCap(gc);

                if (campaignContext)
                {
                    if (cap != -1)
                    {
                        gc.CampaignState.SetWinnersPot(Mathf.Max(gc.CampaignState.WinnersPot, cap));
                    }
                    return;
                }

                var pot = gc.UI.WinnersPot;
                if (pot == null)
                {
                    return;
                }

                int target = cap == -1 ? pot.CoinAmount + 10 : cap;
                int add = target - pot.CoinAmount;
                if (add > 0)
                {
                    StartCoroutine(AddCoinsCo(cluster, pot, add, cap, false));
                }
            }
            else
            {
                int cap = GetPlayerCap(gc);

                if (campaignContext)
                {
                    gc.CampaignState.SetHealth(cap == -1 ? gc.CampaignState.PlayerMaxHealth : cap);
                    return;
                }

                var stash = GetPlayerZone(gc);
                if (stash == null)
                {
                    return;
                }

                int target = cap == -1 ? stash.CoinAmount + 10 : cap;
                int add = target - stash.CoinAmount;
                if (add > 0)
                {
                    StartCoroutine(AddCoinsCo(cluster, stash, add, cap, false));
                }
            }
        }

        private static int GetWinnersPotCap(GameController gc)
        {
            var match = gc.CurrentMatch;
            if (match != null && match.IsNoLimitMatch)
            {
                return -1;
            }
            return GameController.Config.WinnersPotSize.ModifiedValue;
        }

        private static int GetPlayerCap(GameController gc)
        {
            var match = gc.CurrentMatch;
            if (match != null && match.IsNoLimitMatch)
            {
                return -1;
            }
            return gc.CampaignState != null ? gc.CampaignState.PlayerMaxHealth : GameController.Config.PlayerMaxMoney.ModifiedValue;
        }

        private static CoinZone GetPlayerZone(GameController gc)
        {
            var state = gc.State;
            if (state == null || state.Player == null)
            {
                return null;
            }
            return state.Player.Stash;
        }

        // ------------------------------------------------------------------ coin operations

        private IEnumerator AddCoinsCo(Cluster cluster, CoinZone zone, int amount, int liveCap, bool ignoreCap)
        {
            int effectiveCap = ignoreCap || liveCap == -1 ? -1 : liveCap;
            zone.MaxCoinAmount = effectiveCap;

            yield return zone.CreateCoinsWithEffect(amount, null, false,
                cluster == Cluster.WinnersPot ? CoinAnimator.AudioType.CreateInWinnersPot : CoinAnimator.AudioType.Win);

            zone.MaxCoinAmount = liveCap;
            zone.UpdateCoinCountText();
            SyncCampaignState(cluster, zone);
        }

        private void RemoveCoinsFromZone(Cluster cluster, CoinZone zone, int amount)
        {
            var coins = zone.AllCoins;
            if (coins == null || coins.Length == 0)
            {
                return;
            }

            var toRemove = new List<CoinVisual>(amount);

            // Prefer removing regular coins, top of the stack first.
            for (int i = coins.Length - 1; i >= 0 && toRemove.Count < amount; i--)
            {
                var coin = coins[i];
                if (coin == null || coin.coinScriptableObject == null)
                {
                    continue;
                }
                if (!coin.coinScriptableObject.IsSoulCoin)
                {
                    toRemove.Add(coin);
                }
            }

            // Only fall back to soul coins if there is nothing else left.
            for (int i = coins.Length - 1; i >= 0 && toRemove.Count < amount; i--)
            {
                var coin = coins[i];
                if (coin == null || coin.coinScriptableObject == null)
                {
                    continue;
                }
                if (coin.coinScriptableObject.IsSoulCoin && !toRemove.Contains(coin))
                {
                    toRemove.Add(coin);
                }
            }

            if (toRemove.Count == 0)
            {
                return;
            }

            StartCoroutine(RemoveCoinsCo(cluster, zone, toRemove));
        }

        private void RemoveAllCoinsFromZone(Cluster cluster, CoinZone zone)
        {
            var coins = zone.AllCoins;
            if (coins == null || coins.Length == 0)
            {
                return;
            }

            var toRemove = new List<CoinVisual>(coins.Length);
            foreach (var coin in coins)
            {
                if (coin != null)
                {
                    toRemove.Add(coin);
                }
            }

            if (toRemove.Count == 0)
            {
                return;
            }

            StartCoroutine(RemoveCoinsCo(cluster, zone, toRemove));
        }

        private IEnumerator RemoveCoinsCo(Cluster cluster, CoinZone zone, List<CoinVisual> coins)
        {
            yield return zone.DestroyCoinsWithAnimation(coins.ToArray());
            SyncCampaignState(cluster, zone);
        }

        private static void SyncCampaignState(Cluster cluster, CoinZone zone)
        {
            var gc = GameController.Instance;
            if (gc == null || gc.CampaignState == null || zone == null)
            {
                return;
            }

            if (cluster == Cluster.WinnersPot)
            {
                gc.CampaignState.SetWinnersPot(zone.CoinAmount);
            }
            else
            {
                gc.CampaignState.SetHealth(zone.CoinAmount);
            }
        }
    }
}

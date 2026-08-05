using System;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using UnityEngine;

namespace ConnectionSettingsRando
{
    public class ConnectionMenu 
    {
        // Top-level definitions
        internal static ConnectionMenu Instance { get; private set; }
        private readonly SmallButton pageRootButton;

        // Menu page and elements
        private readonly MenuPage accessPage;
        private MenuElementFactory<CSRSettings> topLevelElementFactory;
        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += () => Instance = null;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            button = Instance.pageRootButton;
            button.Text.color = RandoInterop.ConnectionSettings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            return true;
        }

        private static void ConstructMenu(MenuPage connectionPage)
        {
            Instance = new(connectionPage);
        }

        private ConnectionMenu(MenuPage connectionPage)
        {
            // Define connection page
            accessPage = new MenuPage("accessPage", connectionPage);
            
            topLevelElementFactory = new(accessPage, RandoInterop.ConnectionSettings);
            VerticalItemPanel topLevelPanel = new(accessPage, new Vector2(0, 400), 60, true, topLevelElementFactory.Elements);
            topLevelElementFactory.ElementLookup[nameof(CSRSettings.Enabled)].SelfChanged += EnableSwitch;
            topLevelPanel.ResetNavigation();
            topLevelPanel.SymSetNeighbor(Neighbor.Down, accessPage.backButton);
            topLevelPanel.SymSetNeighbor(Neighbor.Up, accessPage.backButton);
            pageRootButton = new SmallButton(connectionPage, "ConnectionSettingsRando");
            pageRootButton.AddHideAndShowEvent(connectionPage, accessPage);
        }

        // Define parameter changes
        private void EnableSwitch(IValueElement obj)
        {
            pageRootButton.Text.color = RandoInterop.ConnectionSettings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        }

        private void SetButtonColor(SmallButton target, Func<bool> condition)
        {
            target.Parent.BeforeShow += () =>
            {
                target.Text.color = condition() ? Colors.TRUE_COLOR : Colors.FALSE_COLOR;
            };
        }

        // Apply proxy settings
        public void Disable()
        {
            IValueElement elem = topLevelElementFactory.ElementLookup[nameof(CSRSettings.Enabled)];
            elem.SetValue(false);
        }

        public void Apply(CSRSettings settings)
        {
            topLevelElementFactory.SetMenuValues(settings);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly SmallButton randomizedSettingsButton;
        // Menu page and elements
        private readonly MenuPage basePage;
        private MenuLabel pageLabel;
        private SmallButton previousButton;
        private SmallButton nextButton;
        private const int Columns = 3;
        private const int Rows = 10;
        private const int PageSize = Columns * Rows;
        private int currentPage = 1;
        private int MaxPage =>
            Math.Max(
                1,
                ((ConnectionsRegistry.Providers.Count() - 1) / PageSize) + 1);
        private MenuElementFactory<CSRSettings> topLevelElementFactory;
        private List<SmallButton> providerButtons = [];
        private List<ISettingsProvider> displayedProviders = [];
        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += () => Instance = null;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            button = Instance.pageRootButton;
            button.Text.color = RandoInterop.Settings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            return true;
        }

        private static void ConstructMenu(MenuPage connectionPage)
        {
            Instance = new(connectionPage);
        }

        private ConnectionMenu(MenuPage connectionPage)
        {
            // Define connection page
            basePage = new MenuPage("basePage", connectionPage);
            randomizedSettingsButton =
                new(
                    basePage,
                    "Active Connections");
            randomizedSettingsButton.AddHideAndShowEvent(DisplayConnections());
            SetButtonColor(randomizedSettingsButton, () => RandoInterop.Settings.RandomizedSettings.Count() > 0);

            topLevelElementFactory = new(basePage, RandoInterop.Settings);
            VerticalItemPanel topLevelPanel = new(basePage, new Vector2(0, 400), 60, true, topLevelElementFactory.Elements);
            topLevelElementFactory.ElementLookup[nameof(CSRSettings.Enabled)].SelfChanged += EnableSwitch;

            topLevelPanel.Add(randomizedSettingsButton);
            topLevelPanel.ResetNavigation();
            topLevelPanel.SymSetNeighbor(Neighbor.Down, basePage.backButton);
            topLevelPanel.SymSetNeighbor(Neighbor.Up, basePage.backButton);
            pageRootButton = new SmallButton(connectionPage, "ConnectionSettingsRando");
            pageRootButton.AddHideAndShowEvent(connectionPage, basePage);
        }

        private MenuPage DisplayConnections()
        {
            MenuPage providersPage = new("Randomized Settings", basePage);
            for (int i = 0; i < PageSize; i++)
            {
                SmallButton button = new(providersPage, "");

                int slot = i;

                button.OnClick += () =>
                {
                    ISettingsProvider provider = displayedProviders[slot];

                    if (provider != null)
                        ToggleProvider(provider);
                };

                providerButtons.Add(button);
                displayedProviders.Add(null);
            }

            VerticalItemPanel root = new(providersPage, new Vector2(0, 350), 75, false);
            for (int row = 0; row < Rows; row++)
            {
                VerticalItemPanel leftHolder = new(providersPage, Vector2.zero, 60, false,
                    providerButtons[row * Columns]);
                VerticalItemPanel middleHolder = new(providersPage, Vector2.zero, 60, false,
                    providerButtons[row * Columns + 1]);
                VerticalItemPanel rightHolder = new(providersPage, Vector2.zero, 60, false,
                    providerButtons[row * Columns + 2]);
                GridItemPanel providerGrid = new(providersPage, new Vector2(0, 350), 3, 0, 500,
                    false, [leftHolder, middleHolder, rightHolder]);
                root.Add(providerGrid);
            }
            pageLabel = new(providersPage, $"Page {currentPage} / {MaxPage}");
            previousButton =
                new(providersPage, "< Previous");
            nextButton =
                new(providersPage, "Next >");
            GridItemPanel navigationGrid = new(providersPage, new Vector2(0, 350), 3, 0, 250, false,
                [
                    new VerticalItemPanel(providersPage, Vector2.zero, 60, false, [previousButton]),
                    new VerticalItemPanel(providersPage, Vector2.zero, 60, false, [pageLabel]),
                    new VerticalItemPanel(providersPage, Vector2.zero, 60, false, [nextButton])
                ]);
            previousButton.OnClick += PreviousPage;
            nextButton.OnClick += NextPage;
            root.Add(navigationGrid);
            root.ResetNavigation();
            root.SymSetNeighbor(Neighbor.Up, providersPage.backButton);
            //root.SymSetNeighbor(Neighbor.Down, providersPage.backButton);
            RefreshProviderPage();
            return providersPage;
        }

        private void RefreshProviderPage()
        {
            List<ISettingsProvider> providers =
                ConnectionsRegistry.Providers
                    .OrderBy(p => p.Name)
                    .ToList();

            int start = (currentPage - 1) * PageSize;
            for (int slot = 0; slot < PageSize; slot++)
            {
                SmallButton button = providerButtons[slot];

                int index = start + slot;

                if (index >= providers.Count)
                {
                    displayedProviders[slot] = null;

                    button.Text.text = "";
                    button.GameObject.SetActive(false);

                    continue;
                }

                ISettingsProvider provider = providers[index];

                displayedProviders[slot] = provider;

                button.GameObject.SetActive(true);

                button.Text.text = provider.Name;
                button.Text.color =
                    IsProviderEnabled(provider)
                        ? Colors.TRUE_COLOR
                        : Colors.DEFAULT_COLOR;
            }
            RefreshPageButtons();
            
        }

        private void RefreshPageButtons()
        {
            pageLabel.Text.text = $"Page {currentPage} / {MaxPage}";
            pageLabel.GameObject.SetActive(MaxPage > 1);
            previousButton.GameObject.SetActive(currentPage > 1);
            nextButton.GameObject.SetActive(currentPage < MaxPage);
        }

        // Define parameter changes
        private void EnableSwitch(IValueElement obj)
        {
            pageRootButton.Text.color = RandoInterop.Settings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;   
        }

        private void SetButtonColor(SmallButton target, Func<bool> condition)
        {
            target.Parent.BeforeShow += () =>
            {
                target.Text.color = condition() ? Colors.TRUE_COLOR : Colors.FALSE_COLOR;
            };
        }
        private bool IsProviderEnabled(
            ISettingsProvider provider)
        {
            return RandoInterop.Settings.RandomizedSettings
                .Contains(provider.Name);
        }

        private void ToggleProvider(ISettingsProvider provider)
        {
            List<string> enabled =
                RandoInterop.Settings.RandomizedSettings;

            if (enabled.Contains(provider.Name))
                enabled.Remove(provider.Name);
            else
                enabled.Add(provider.Name);
            RefreshProviderPage();
            SetButtonColor(randomizedSettingsButton, () => RandoInterop.Settings.RandomizedSettings.Count() > 0);
        }

        private void PreviousPage()
        {
            if (currentPage > 1)
            {
                currentPage--;
                RefreshProviderPage();
            }
        }
        private void NextPage()
        {
            if (currentPage < MaxPage)
            {
                currentPage++;
                RefreshProviderPage();
            }
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
            RefreshProviderPage();
            randomizedSettingsButton.Text.color =
                settings.RandomizedSettings.Count > 0 ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        }
    }
}
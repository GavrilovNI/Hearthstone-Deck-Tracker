﻿using Hearthstone_Deck_Tracker.Hearthstone;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Utility.Battlegrounds;
using static Hearthstone_Deck_Tracker.Utility.Battlegrounds.BattlegroundsLastGames;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Collections.Generic;

namespace Hearthstone_Deck_Tracker.Controls.Overlay
{
	public partial class BattlegroundsSession : UserControl
	{
		private Lazy<BattlegroundsDb> _db = new Lazy<BattlegroundsDb>();
		public ObservableCollection<BattlegroundsGame> Games { get; set; } = new ObservableCollection<BattlegroundsGame>();

		public BattlegroundsSession()
		{
			InitializeComponent();
		}

		private void Update()
		{
			var allRaces = _db.Value.Races;
			var availableRaces = BattlegroundsUtils.GetAvailableRaces(Core.Game.CurrentGameStats?.GameId) ?? allRaces;
			var unavailableRaces = allRaces.Where(x => !availableRaces.Contains(x) && x != Race.INVALID && x != Race.ALL).ToList();

			if(unavailableRaces.Count() >= 3)
			{
				BgTribe1.Tribe = unavailableRaces[0];
				BgTribe2.Tribe = unavailableRaces[1];
				BgTribe3.Tribe = unavailableRaces[2];
				if(unavailableRaces.Count() == 4)
				{
					BgTribe4.Tribe = unavailableRaces[3];
				}
				else
				{
					BgBannedTribes.Children.Remove(BgTribe4);
					BgTribe2.Margin = new Thickness(15, 0, 0, 0);
					BgTribe3.Margin = new Thickness(15, 0, 0, 0);
				}
			}

			var firstGame = UpdateLatestGames();

			var rating = Core.Game.BattlegroundsRatingInfo?.Rating;
			var ratingStart = firstGame?.RatingAfter ?? rating;
			BgRatingStart.Text = $"{ratingStart:N0}";
			BgRatingCurrent.Text = $"{rating:N0}";
		}

		private void BtnOptions_MouseUp(object sender, RoutedEventArgs e)
		{
			Core.MainWindow.ActivateWindow();
			Core.MainWindow.FlyoutOptions.IsOpen = true;
		}

		private void BtnOptions_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			BtnOptions.Background = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255));
		}

		private void BtnOptions_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			BtnOptions.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
		}

		public void Show()
		{
			if (Visibility == Visibility.Visible)
			{
				return;
			}
			Update();
			UpdateSectionsVisibilities();
			Visibility = Visibility.Visible;
		}

		public void Hide()
		{
			Visibility = Visibility.Hidden;
		}

		public GameItem? UpdateLatestGames()
		{
			Games.Clear();
			var sortedGames = BattlegroundsLastGames.Instance.Games
				.OrderBy(g => g.StartTime)
				.ToList();

			var sessionGames = getSessionGames(sortedGames);

			// Limit list to latest 10 items
			if(sessionGames.Count > 10)
				sessionGames.RemoveRange(0, sessionGames.Count - 10);

			sessionGames.ForEach(AddOrUpdateGame);

			GridHeader.Visibility = sessionGames.Count > 0
				? Visibility.Visible
				: Visibility.Collapsed;

			GamesEmptyState.Visibility = sessionGames.Count == 0
				? Visibility.Visible
				: Visibility.Collapsed;

			return sessionGames.FirstOrDefault();
		}

		private List<GameItem> getSessionGames(List<GameItem> sortedGames)
		{
			DateTime? sessionStartTime = null;
			DateTime? previousGameEndTime = null;
			foreach (var g in sortedGames)
			{
				if(previousGameEndTime != null)
				{
					var gStartTime = DateTime.Parse(g.StartTime);
					TimeSpan ts = gStartTime - (DateTime)previousGameEndTime;
					if(ts.TotalHours >= 2)
						sessionStartTime = gStartTime;

				}
				previousGameEndTime = DateTime.Parse(g.EndTime);
			};
			return sortedGames.Where(g => DateTime.Parse(g.StartTime) >= sessionStartTime).ToList();
		}

		private void AddOrUpdateGame(GameItem game)
		{
			var existingGame = Games.FirstOrDefault(x => x?.Game?.StartTime == game.StartTime);
			if (existingGame == null)
			{
				Games.Add(new BattlegroundsGame() { Game = game });
			}
			else
			{
				existingGame.Game = game;
			}
		}

		public void UpdateSectionsVisibilities()
		{
			BgBannedTribesSection.Visibility = Config.Instance.ShowSessionRecapMinionsBanned
				? Visibility.Visible
				: Visibility.Collapsed;

			BgStartCurrentMMRSection.Visibility = Config.Instance.ShowSessionRecapStartCurrentMMR
				? Visibility.Visible
				: Visibility.Collapsed;

			BgLastestGamesSection.Visibility = Config.Instance.ShowSessionRecapLatestGames
				? Visibility.Visible
				: Visibility.Collapsed;
		}
	}
}

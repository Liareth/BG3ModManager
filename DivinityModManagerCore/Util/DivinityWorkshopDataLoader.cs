﻿using DivinityModManager.Models;
using DivinityModManager.Models.Steam;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Util
{
	public static class DivinityWorkshopDataLoader
	{
		public static readonly string STEAM_API_GET_WORKSHOP_DATA_URL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/?";

		private static string CreatePublishFileIds(List<DivinityModData> mods)
		{
			string output = "";
			for(int i = 0; i < mods.Count; i++)
			{
				var mod = mods[i];
				output += output + $"publishedfileids[{i}]: {mod.WorkshopData.ID}";
				if (i < mods.Count - 1) output += ", ";
			}
			return output;
		}

		private static readonly HttpClient client = new HttpClient();

		public static async Task<Unit> LoadAllWorkshopDataAsync(List<DivinityModData> workshopMods)
		{
			//var workshopMods = mods.Where(x => !String.IsNullOrEmpty(x.WorkshopData.ID)).ToList();
			var values = new Dictionary<string, string>
			{
			{ "itemcount", workshopMods.Count.ToString()}
			};
			int i = 0;
			foreach (var mod in workshopMods)
			{
				values.Add($"publishedfileids[{i}]", mod.WorkshopData.ID);
				i = i + 1;
			}

			Trace.WriteLine($"Attempting to get workshop data from mods.");

			var content = new FormUrlEncodedContent(values);
			var response = await client.PostAsync(STEAM_API_GET_WORKSHOP_DATA_URL, content);
			var responseData = await response.Content.ReadAsStringAsync();

			if (!String.IsNullOrEmpty(responseData))
			{
				PublishedFileDetailsResponse pResponse = DivinityJsonUtils.SafeDeserialize<PublishedFileDetailsResponse>(responseData);
				if(pResponse != null && pResponse.response != null && pResponse.response.publishedfiledetails != null && pResponse.response.publishedfiledetails.Count > 0)
				{
					var details = pResponse.response.publishedfiledetails;
					foreach (var d in details)
					{
						try
						{
							var mod = workshopMods.FirstOrDefault(x => x.WorkshopData.ID == d.publishedfileid);
							if (mod != null)
							{
								if(d.tags != null && d.tags.Count > 0) mod.WorkshopData.Tags = d.tags.Select(x => x.tag).ToList();
								mod.WorkshopData.PreviewUrl = d.preview_url;
								mod.WorkshopData.Title = d.title;
								mod.WorkshopData.Description = d.description;
								mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(d.time_created);
								mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(d.time_updated);
								mod.WorkshopData.Subscriptions = d.subscriptions;
								mod.WorkshopData.LifetimeSubscriptions = d.lifetime_subscriptions;
								mod.WorkshopData.Favorites = d.favorited;
								mod.WorkshopData.LifetimeFavorites = d.lifetime_favorited;
								mod.WorkshopData.Views = d.views;

								Trace.WriteLine($"Loaded workshop details for mod {mod.Name}:");
								Trace.WriteLine($"Tags: {String.Join(";", mod.WorkshopData.Tags)}");
							}
						}
						catch(Exception ex)
						{
							Trace.WriteLine($"Error parsing mod data for {d.title}({d.publishedfileid})\n{ex.ToString()}");
						}
					}
				}
				else
				{
					Trace.WriteLine("Response data parsing failed!");
					Trace.WriteLine($"{responseData}");
				}
			}
			else
			{
				Trace.WriteLine($"Response data is null!");
				//throw new Exception("Response data is null!");
			}
			return Unit.Default;
		}
	}
}

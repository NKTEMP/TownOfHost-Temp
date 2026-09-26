//TOH_Yを参考にさせて貰いました ありがとうございます
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data.Player;
using Assets.InnerNet;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using TownOfHost;
using UnityEngine.Networking;

[HarmonyPatch]
public class ModNewsHistory
{
    public static List<ModNews> AllModNews = new();
    public static List<ModNews> JsonAndAllModNews = new();
    public static void Init()
    {
        AllModNews.Clear();
        JsonAndAllModNews.Clear();

        AllModNews.Add(new ModNews
        {
            Number = 100084,
            Title = "Town Of Host-Temp 公開！",
            SubTitle = "<color=#00c1ff>Town Of Host-Temp 初回公開</color>",
            ShortTitle = "<color=#00c1ff>◆TOH-Tm 初回公開</color>",
            Text = "<size=80%>Town Of Host-Tempを公開しました！\n\n<size=125%>【追加役職】</size>\n・鍵師\n・歩爆魔\n・検死官\n\n<size=125%>【削除役職】</size>\n・爆弾魔\n\n遊んでくれてありがとうございます。\n不具合を見つけた場合はDiscordで報告してください。</size>",
            Date = "2026-09-23T12:00:00Z"
        });

        AllModNews.Add(new ModNews
        {
            Number = 100085,
            Title = "v3.17.32.54 公開！",
            SubTitle = "<color=#00c1ff>Town Of Host-Temp v3.17.32.54</color>",
            ShortTitle = "<color=#00c1ff>◆TOH-Tm v3.17.32.54</color>",
            Text = "<size=80%>Town Of Host-Temp v3.17.32.54を公開しました！\n\n<size=125%>【修正】</size>\n・鍵師のバグを修正\n\n<size=125%>【追加】</size>\n・マッチメイキング機能を追加\n・Discordの募集情報に参加人数、ルームコード、ホスト名、リージョンを表示\n・公開中の募集を更新、非公開化や部屋閉鎖時に削除する機能を追加\n\n不具合を見つけた場合はDiscordで報告してください。</size>",
            Date = "2026-09-26T08:00:00Z"
        });

        AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
    }
    //ここもTownOfHost_Y様を参考に..!
    public const string ModNewsURL = "https://raw.githubusercontent.com/NKTEMP/TownOfHost-Temp/refs/heads/main/ModNews.json";
    static bool downloaded = false;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    public static void StartPostfix(MainMenuManager __instance)
    {
        static IEnumerator FetchModNews()
        {
            if (downloaded)
            {
                yield break;
            }
            downloaded = true;
            var request = UnityWebRequest.Get(ModNewsURL);
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                downloaded = false;
                TownOfHost.Logger.Info("ModNews Error Fetch:" + request.responseCode.ToString(), "ModNews");
                yield break;
            }
            var json = JObject.Parse(request.downloadHandler.text);
            for (var news = json["News"].First; news != null; news = news.Next)
            {
                JsonModNews n = new(
                    int.Parse(news["Number"].ToString()), news["Title"]?.ToString(), news["Subtitle"]?.ToString(), news["Short"]?.ToString(),
                    news["Body"]?.ToString(), news["Date"]?.ToString());
            }
        }
        __instance.StartCoroutine(FetchModNews().WrapToIl2Cpp());
    }

    [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
    public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
    {
        if (AllModNews.Count < 1)
        {
            Init();
            AllModNews.Do(n => JsonAndAllModNews.Add(n));
            JsonAndAllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
        }

        List<Announcement> FinalAllNews = new();
        JsonAndAllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
        foreach (var news in aRange)
        {
            if (!JsonAndAllModNews.Any(x => x.Number == news.Number))
                FinalAllNews.Add(news);
        }
        FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

        aRange = new(FinalAllNews.Count);
        for (int i = 0; i < FinalAllNews.Count; i++)
            aRange[i] = FinalAllNews[i];

        return true;
    }
}

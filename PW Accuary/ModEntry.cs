using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnityEngine.UI;
using TMPro;
using Harmony;
using Newtonsoft.Json;

namespace PW_Accuary
{
    public class ModEntry : MelonMod
    {
        const string configPath = "Mods/PW Accuracy/config.json";

        static GameData data = null;
        static bool updateAccDisplay = false;
        static int onBeatHits = 0;
        static int totalHits = 0;

        static Config config;

        public override void OnApplicationStart()
        {
            if (System.IO.File.Exists(configPath))
            {
                string json = System.IO.File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<Config>(json);
                MelonLogger.Log("Loaded configuration for PW Accuracy");
            }
            else
            {
                config = new Config();
                MelonLogger.LogWarning($"No configuration file exists at {configPath}. Default configuration is loaded");
            }
            MelonLogger.Log("PW Accuracy configuration:");
            MelonLogger.Log(config.ToString());
        }

        [HarmonyPatch(typeof(PlayerActionManager), "OnGameStart", new System.Type[0] { })]
        public static class PlayerActionManagerGameStartMod
        {
            public static void Postfix(PlayerActionManager __instance)
            {
                data = __instance.playerData;

            }
        }

        [HarmonyPatch(typeof(PlayerActionManager), "OnGameEnd", new System.Type[0] { })]
        public static class PlayerActionManagerEndGameMod
        {
            public static void Postfix(PlayerActionManager __instance)
            {
                onBeatHits = 0;
                totalHits = 0;
                updateAccDisplay = true; //Trigger update of UI when we move into scene selection again
            }
        }



        [HarmonyPatch(typeof(GunAmmoDisplay), "Update", new System.Type[0] { })]
        public static class GunAmmoDisplayMod
        {
            
            public static void Postfix(GunAmmoDisplay __instance)
            {
                //Are we not playing, then there is no reason to display accuracy text
                if (GameManager.Instance.playing == false)
                {
                    if (updateAccDisplay == true)
                    {
                        TextMeshPro text = __instance.displayText;
                        text.text = $"<size={config.bulletCountValueSize}%>{__instance.currentBulletCount}";
                        updateAccDisplay = false;
                    }

                    return;
                }

                if (data == null)
                    return;

                if (updateAccDisplay == true)
                {
                    TextMeshPro text = __instance.displayText;
                    string richText = "";
                    richText += $"<size={config.bulletCountValueSize}%>{__instance.currentBulletCount}\n";

                    float beatAccuracy = 0;
                    if (totalHits > 0)
                    {
                        beatAccuracy = (float)onBeatHits / (float)totalHits;
                    }

                    if (config.boldedText == true)
                    {
                        richText += $"<size={config.accuracyValueSize}%>{(data.accuracy * 100f):0.00}%<size={config.accuracyTextSize}%><b>{config.accuracyString}</b>\n";
                        richText += $"<size={config.beatAccuracyValueSize}%>{(beatAccuracy * 100f):0.00}%<size={config.beatAccuracyTextSize}%><b>{config.beatAccuracyString}</b>";
                    }
                    else
                    {
                        richText += $"<size={config.accuracyValueSize}%>{(data.accuracy * 100f):0.00}%<size={config.accuracyTextSize}%>{config.accuracyString}\n";
                        richText += $"<size={config.beatAccuracyValueSize}%>{(beatAccuracy * 100f):0.00}%<size={config.beatAccuracyTextSize}%>{config.beatAccuracyString}";
                    }
                    
                    text.text = richText;
                    text.richText = true;

                    updateAccDisplay = false;
                }
            }
        }

        [HarmonyPatch(typeof(Gun), "Fire", new System.Type[0] { })]
        public static class GunFireMod
        {

            public static void Postfix(Gun __instance)
            {
                updateAccDisplay = true;
            }
        }

        [HarmonyPatch(typeof(Gun), "Reload", new System.Type[1] {typeof(bool)})]
        public static class GunReloadMod
        {

            public static void Postfix(Gun __instance, bool triggeredByMelee)
            {
                updateAccDisplay = true;
            }
        }


        [HarmonyPatch(typeof(GameData), "AddScore", new System.Type[1] { typeof(ScoreItem) })]
        public static class GameDataMod
        {

            public static void Prefix(Gun __instance, ScoreItem score)
            {

                //MelonLogger.Log($"onBeat: {score.onBeatValue}, accuracyValue: {score.accuracy}");
                if (score.onBeatValue == 100)
                    onBeatHits++;
                totalHits++;
                updateAccDisplay = true;
            }
        }
    }

    class Config
    {
        public string accuracyString;
        public string beatAccuracyString;
        public bool boldedText;
        public int accuracyValueSize;
        public int beatAccuracyValueSize;
        public int accuracyTextSize;
        public int beatAccuracyTextSize;
        public int bulletCountValueSize;

        public Config(string accuracyString, string beatAccuracyString, bool boldedText, int accuracyValueSize, int beatAccuracyValueSize, int accuracyTextSize, int beatAccuracyTextSize, int bulletCountValueSize)
        {
            this.accuracyString = accuracyString;
            this.beatAccuracyString = beatAccuracyString;
            this.boldedText = boldedText;
            this.accuracyValueSize = accuracyValueSize;
            this.beatAccuracyValueSize = beatAccuracyValueSize;
            this.accuracyTextSize = accuracyTextSize;
            this.beatAccuracyTextSize = beatAccuracyTextSize;
            this.bulletCountValueSize = bulletCountValueSize;
        }

        public Config()
        {
            this.accuracyString = " Acc";
            this.beatAccuracyString = " Beat";
            this.boldedText = true;
            this.accuracyValueSize = 40;
            this.beatAccuracyValueSize = 40;
            this.accuracyTextSize = 50;
            this.beatAccuracyTextSize = 50;
            this.bulletCountValueSize = 100;
        }

        public override string ToString()
        {
            string str = "\n";
            str += $"accuracyString: {accuracyString}\n";
            str += $"beatAccuracyString: {beatAccuracyString}\n";
            str += $"boldedText: {boldedText}\n";
            str += $"accuracyValueSize: {accuracyValueSize}\n";
            str += $"beatAccuracyValueSize: {beatAccuracyValueSize}\n";
            str += $"accuracyTextSize: {accuracyTextSize}\n";
            str += $"beatAccuracyTextSize: {beatAccuracyTextSize}\n";
            str += $"bulletCountValueSize: {bulletCountValueSize}\n";

            return str;
        }
    }
}

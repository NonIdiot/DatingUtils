using BepInEx;
using UnityEngine;
using Expedition;
using System;
using Menu;
using Menu.Remix.MixedUI;
using System.Linq;
using BepInEx.Logging;
using JollyCoop.JollyMenu;
using MSCSceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using System.Runtime.CompilerServices;
using System.Runtime;
using HUD;
using RWCustom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static MonoMod.InlineRT.MonoModRule;
using JetBrains.Annotations;
using SlugBase;
using IL.JollyCoop.JollyManual;
using HarmonyLib;
using On.MoreSlugcats;
using RainMeadow;
using Unity.Mathematics;
// ReSharper disable SimplifyLinqExpressionUseAll

// ReSharper disable UseMethodAny.0

// ReSharper disable once CheckNamespace
namespace DatingUtils
{
    public static class GeneralCWT
    {
        static ConditionalWeakTable<SimpleButton, Data> table = new ConditionalWeakTable<SimpleButton, Data>();
        public static Data GetCustomData(this SimpleButton self) => table.GetOrCreateValue(self);

        public class Data
        {
            // stored simplebutton stuff
            public int typeOf = -1;
            public string whichVar = "-1";
            public int setToWhat = -1;
            public string[] whichVarInVarSet;
            public int[] setToWhatInVarSet;
            public bool dead = false;
        }
    }

    [BepInPlugin(MOD_ID, "NonIdiot's Dating Sim Utils", "0.0.1")]
    internal class Plugin : BaseUnityPlugin
    {
        public const string MOD_ID = "nassoc.datingutils";
        
        public Dictionary<string, int> allVariables = new Dictionary<string, int>();

        // thank you alphappy for logging help too
        internal static BepInEx.Logging.ManualLogSource logger;
        internal static void Log(LogLevel loglevel, object msg) => logger.Log(loglevel, msg);

        internal static Plugin instance;
        public Plugin()
        {
            logger = Logger;
            instance = this;
        }

        public string[][] losOverlaysData = [];
        public MenuIllustration[] losOverlays = [];
        
        private bool weInitializedYet = false;
        public void OnEnable()
        {
            try
            {
                Logger.LogDebug("NonIdiot's Dating Sim Utils Plugin loading...");
                //On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

                if (!weInitializedYet)
                {
                    On.RainWorld.PostModsInit += PostModsInitt;

                    On.RainWorld.OnModsInit += RainWorldOnModsInitHook;

                    On.MoreSlugcats.DatingSim.InitNextFile += IInitNextFile;
                    On.MoreSlugcats.DatingSim.Singal += IPressButton;
                    On.MoreSlugcats.DatingSim.NewMessage_string_int += INewMessage;
                }

                weInitializedYet = true;
                Logger.LogDebug("NonIdiot's Dating Sim Utils Plugin successfully loaded!");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void IInitNextFile(DatingSim.orig_InitNextFile orig, MoreSlugcats.DatingSim self, string filename)
        {
            if (filename == "start.txt")
            {
                for (int i = 0; i < losOverlays.Length; i++)
                {
                    losOverlays[i].RemoveSprites();
                    self.pages[0].subObjects.Remove(losOverlays[i]);
                }

                losOverlays = [];
                losOverlaysData = [];
                Plugin.Log(LogLevel.Info, "[NonIdiot's DatingUtils] All Overlaysets reset!");
            }

            orig(self, filename);
            
            if (filename == "start.txt")
            {
                allVariables = new Dictionary<string, int>();
                Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Set all variables to a default of 0");
            }
            
            if (!self.creditsLabelLeft.text.Contains("Additional functionality added by NonIdiot"))
            {
                self.creditsLabelLeft.text += Environment.NewLine+"Additional functionality added by NonIdiot";
            }
            
            // Variable checking, setting, etc.
            for (int i = 0; i < self.messageButtons.Count; i++)
            {
                char[] myArray = self.messageButtons[i].menuLabel.text.ToCharArray();
                bool[] isWorking = [true,true,true,true];
                int[] sset = [5,8,7,10];
                for (int j = 1; j < Math.Min(myArray.Length,9); j++)
                {
                    if (j <= 3 && myArray[j] != "[var".ToCharArray()[j])
                    {
                        isWorking[0] = false;
                    }
                    if (j <= 6 && myArray[j] != "[varset".ToCharArray()[j])
                    {
                        isWorking[1] = false;
                    }
                    if (j <= 5 && myArray[j] != "[check".ToCharArray()[j])
                    {
                        isWorking[2] = false;
                    }
                    if (j <= 8 && myArray[j] != "[checkset".ToCharArray()[j])
                    {
                        isWorking[3] = false;
                    }

                    if (isWorking == new[] {false, false, false, false})
                    {
                        Log("im not doing moss " + self.messageButtons[i].menuLabel.text);
                        break;
                    }
                }

                if (!self.messageButtons[i].GetCustomData().dead && isWorking != new[] {false, false, false, false} && self.messageButtons[i].menuLabel.text.ToCharArray()[0].ToString() == "[")
                {
                    int numm = -1;
                    for (var j = 3; j > -1; j--)
                    {
                        if (isWorking[j])
                        {
                            numm = j;
                            break;
                        }
                    }

                    bool begoneThee = false;
                    self.messageButtons[i].GetCustomData().typeOf = numm;
                    self.messageButtons[i].GetCustomData().whichVar = stringToSmol(string.Join("", myArray).Trim(), sset[numm], filename);
                    //Logger.Log(LogLevel.Info, "EEE " + numm + " " + self.messageButtons[i].GetCustomData().whichVar + " " + string.Join("", myArray).Trim() + " " + sset[numm]);
                    if (numm == 0 || numm == 2)
                    {
                        self.messageButtons[i].GetCustomData().setToWhat = stringToNumber(string.Join("", myArray).Trim(), sset[numm] + self.messageButtons[i].GetCustomData().whichVar.Length + 1, filename);
                        if (numm == 2)
                        {
                            int myBul = allVarReturn(self.messageButtons[i].GetCustomData().whichVar);
                            bool myeBool = myBul == self.messageButtons[i].GetCustomData().setToWhat;
                            if (!myeBool)
                            {
                                begoneThee = true;
                            }
                            Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Check \""+self.messageButtons[i].menuLabel.text+"\" (var \""+self.messageButtons[i].GetCustomData().whichVar+"\"=="+self.messageButtons[i].GetCustomData().setToWhat+") came back with "+myeBool+" (var returned "+myBul+")");
                        }
                    }
                    else
                    {
                        self.messageButtons[i].GetCustomData().setToWhat = 0;
                        if (numm == 1)
                        {
                            string path = AssetManager.ResolveFilePath(string.Concat(new string[]
                            {
                                "Content",
                                Path.DirectorySeparatorChar.ToString(),
                                "text_",
                                LocalizationTranslator.LangShort(self.manager.rainWorld.inGameTranslator.currentLanguage),
                                Path.DirectorySeparatorChar.ToString(),
                                "datingutils",
                                Path.DirectorySeparatorChar.ToString(),
                                "varset_",
                                self.messageButtons[i].GetCustomData().whichVar,
                                ".txt",
                            }));
                            string[] fileArray = [];
                            if (File.Exists(path))
                            {
                                fileArray = File.ReadAllLines(path);
                            }
                            else
                            {
                                path = AssetManager.ResolveFilePath(string.Concat(new string[]
                                {
                                    "Content",
                                    Path.DirectorySeparatorChar.ToString(),
                                    "text_",
                                    LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English),
                                    Path.DirectorySeparatorChar.ToString(),
                                    "datingutils",
                                    Path.DirectorySeparatorChar.ToString(),
                                    "varset_",
                                    self.messageButtons[i].GetCustomData().whichVar,
                                    ".txt",
                                }));
                                if (File.Exists(path))
                                {
                                    fileArray = File.ReadAllLines(path);
                                }
                                else
                                {
                                    Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in a nonexistent Varset. Does file \"content/text_"+LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English)+"/datingutils/varset_"+self.messageButtons[i].GetCustomData().whichVar+".txt\" exist?");
                                }
                            }

                            if (fileArray.Length > 0)
                            {
                                self.messageButtons[i].GetCustomData().whichVarInVarSet = [];
                                self.messageButtons[i].GetCustomData().setToWhatInVarSet = [];
                                foreach (string filee in fileArray)
                                {
                                    string returnString = stringToSmol(filee.Trim(), 0, filename);
                                    int returnNum = stringToNumber(filee.Trim(), returnString.Length+1, filename);
                                    self.messageButtons[i].GetCustomData().whichVarInVarSet = self.messageButtons[i].GetCustomData().whichVarInVarSet.AddItem(returnString).ToArray();
                                    self.messageButtons[i].GetCustomData().setToWhatInVarSet = self.messageButtons[i].GetCustomData().setToWhatInVarSet.AddItem(returnNum).ToArray();
                                }
                            }
                        }
                        else if (numm == 3)
                        {
                            self.messageButtons[i].GetCustomData().setToWhat = stringToNumber(string.Join("", myArray).Trim(), sset[numm] + self.messageButtons[i].GetCustomData().whichVar.Length + 1, filename);
                            begoneThee = true;
                            string path = AssetManager.ResolveFilePath(string.Concat(new string[]
                            {
                                "Content",
                                Path.DirectorySeparatorChar.ToString(),
                                "text_",
                                LocalizationTranslator.LangShort(self.manager.rainWorld.inGameTranslator.currentLanguage),
                                Path.DirectorySeparatorChar.ToString(),
                                "datingutils",
                                Path.DirectorySeparatorChar.ToString(),
                                "checkset_",
                                self.messageButtons[i].GetCustomData().whichVar,
                                ".txt",
                            }));
                            string[] fileArray = [];
                            if (File.Exists(path))
                            {
                                fileArray = File.ReadAllLines(path);
                            }
                            else
                            {
                                path = AssetManager.ResolveFilePath(string.Concat(new string[]
                                {
                                    "Content",
                                    Path.DirectorySeparatorChar.ToString(),
                                    "text_",
                                    LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English),
                                    Path.DirectorySeparatorChar.ToString(),
                                    "datingutils",
                                    Path.DirectorySeparatorChar.ToString(),
                                    "checkset_",
                                    self.messageButtons[i].GetCustomData().whichVar,
                                    ".txt",
                                }));
                                if (File.Exists(path))
                                {
                                    fileArray = File.ReadAllLines(path);
                                }
                                else
                                {
                                    Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in a nonexistent Checkset. Does file \"content/text_"+LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English)+"/datingutils/checkset_"+self.messageButtons[i].GetCustomData().whichVar+".txt\" exist?");
                                }
                            }

                            if (fileArray.Length > 0)
                            {
                                begoneThee = false;
                                self.messageButtons[i].GetCustomData().whichVarInVarSet = [];
                                self.messageButtons[i].GetCustomData().setToWhatInVarSet = [];
                                foreach (string filee in fileArray)
                                {
                                    string returnString = stringToSmol(filee, 0, filename);
                                    int returnNum = stringToNumber(filee, returnString.Length+1, filename);
                                    //Logger.Log(LogLevel.Info, "AAAAA "+returnString+" "+returnNum);
                                    self.messageButtons[i].GetCustomData().whichVarInVarSet = self.messageButtons[i].GetCustomData().whichVarInVarSet.AddItem(returnString).ToArray();
                                    self.messageButtons[i].GetCustomData().setToWhatInVarSet = self.messageButtons[i].GetCustomData().setToWhatInVarSet.AddItem(returnNum).ToArray();
                                }

                                //Logger.Log(LogLevel.Info, "AAAAE "+self.messageButtons[i].GetCustomData().whichVarInVarSet.Length);
                                for (int j = 0; j<self.messageButtons[i].GetCustomData().whichVarInVarSet.Length; j++)
                                {
                                    int aBul = allVarReturn(self.messageButtons[i].GetCustomData().whichVarInVarSet[j]);
                                    bool laBool = aBul == self.messageButtons[i].GetCustomData().setToWhatInVarSet[j];
                                    Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Checkset \""+self.messageButtons[i].menuLabel.text+"\" check #"+j+" (var \""+self.messageButtons[i].GetCustomData().whichVarInVarSet[j]+"\"=="+self.messageButtons[i].GetCustomData().setToWhatInVarSet[j]+") came back with "+laBool+" (var returned "+aBul+")");
                                    if (!laBool && self.messageButtons[i].GetCustomData().setToWhat != 1)
                                    {
                                        begoneThee = true;
                                        break;
                                    }
                                    if (laBool && self.messageButtons[i].GetCustomData().setToWhat == 1)
                                    {
                                        begoneThee = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    //numm+" "+
                    //Log("im doing moss "+self.messageButtons[i].menuLabel.text+" "+self.messageButtons[i].GetCustomData().whichVar+" "+self.messageButtons[i].GetCustomData().setToWhat);
                    //self.messageButtons[i].menuLabel.text += "moss";
                    self.messageButtons[i].menuLabel.text = self.messageButtons[i].menuLabel.text.Split("]".ToCharArray())[1];
                    if (begoneThee)
                    {
                        Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Hiding button \""+self.messageButtons[i].menuLabel.text+"\" of type "+numm);
                        self.messageButtons[i].RemoveSprites();
                        self.pages[0].selectables.Remove(self.messageButtons[i]);
                        self.pages[0].subObjects.Remove(self.messageButtons[i]);
                        self.messageButtons[i].GetCustomData().dead = true;
                    }
                }
            }

            // Reformatting buttons to be in the right place
            float num5 = 0f;
            for (int num6 = 0; num6 < self.messageButtons.Count; num6++)
            {
                if (!self.messageButtons[num6].GetCustomData().dead)
                {
                    float num7 = self.messageButtons[num6].menuLabel.label.textRect.width + 20f;
                    num5 += num7 + 10f;
                    self.messageButtons[num6].SetSize(new Vector2(num7, 30f));
                }
            }
            float num8 = 0f;
            for (int num9 = 0; num9 < self.messageButtons.Count; num9++)
            {
                if (!self.messageButtons[num9].GetCustomData().dead)
                {
                    self.messageButtons[num9].pos.x = self.manager.rainWorld.options.ScreenSize.x * 0.5f - num5 * 0.5f +
                                                      num8 + (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f;
                    num8 += self.messageButtons[num9].size.x + 10f;
                }
            }
            
            // Overlay nonsense (aka setup of overlay data)
            string path2 = AssetManager.ResolveFilePath(string.Concat(new string[]
            {
                "Content",
                Path.DirectorySeparatorChar.ToString(),
                "text_",
                LocalizationTranslator.LangShort(self.manager.rainWorld.inGameTranslator.currentLanguage),
                Path.DirectorySeparatorChar.ToString(),
                filename
            }));
            string[] array;
            if (File.Exists(path2))
            {
                array = File.ReadAllLines(path2);
            }
            else
            {
                path2 = AssetManager.ResolveFilePath(string.Concat(new string[]
                {
                    "Content",
                    Path.DirectorySeparatorChar.ToString(),
                    "text_",
                    LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English),
                    Path.DirectorySeparatorChar.ToString(),
                    filename
                }));
                if (File.Exists(path2))
                {
                    array = File.ReadAllLines(path2);
                }
                else
                {
                    array = [];
                    Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in one REALLY broken thingy. Literally how the hell did we get here?!?");
                }
            }

            bool bonanar = true;
            string woga = "[overlayset ";
            if (woga.Length+2 > array[3].Length)
            {
                bonanar = false;
            }
            else
            {
                for (int i=0;i<woga.Length;i++)
                {
                    if (array[3][i] != woga[i])
                    {
                        bonanar = false;
                        break;
                    }
                }
            }
            if (bonanar)
            {
                if (array[3][12].ToString() == "-" && array[3][13].ToString() == "1")
                {
                    for (int i = 0; i < losOverlays.Length; i++)
                    {
                        losOverlays[i].RemoveSprites();
                        self.pages[0].subObjects.Remove(losOverlays[i]);
                    }
                    losOverlays = [];
                    losOverlaysData = [];
                    Plugin.Log(LogLevel.Info, "[NonIdiot's DatingUtils] All Overlaysets reset!");
                }
                else
                {
                    string boogeru = stringToSmol(array[3], 12, filename);
                    string path = AssetManager.ResolveFilePath(string.Concat(new string[]
                    {
                        "Content",
                        Path.DirectorySeparatorChar.ToString(),
                        "text_",
                        LocalizationTranslator.LangShort(self.manager.rainWorld.inGameTranslator.currentLanguage),
                        Path.DirectorySeparatorChar.ToString(),
                        "datingutils",
                        Path.DirectorySeparatorChar.ToString(),
                        "overlayset_",
                        boogeru,
                        ".txt",
                    }));
                    string[] fileArray = [];
                    if (File.Exists(path))
                    {
                        fileArray = File.ReadAllLines(path);
                    }
                    else
                    {
                        path = AssetManager.ResolveFilePath(string.Concat(new string[]
                        {
                            "Content",
                            Path.DirectorySeparatorChar.ToString(),
                            "text_",
                            LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English),
                            Path.DirectorySeparatorChar.ToString(),
                            "datingutils",
                            Path.DirectorySeparatorChar.ToString(),
                            "overlayset_",
                            boogeru,
                            ".txt",
                        }));
                        if (File.Exists(path))
                        {
                            fileArray = File.ReadAllLines(path);
                        }
                        else
                        {
                            Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in a nonexistent Overlayset. Does file \"content/text_"+self.manager.rainWorld.inGameTranslator.currentLanguage+"/datingutils/overlayset_"+boogeru+".txt\" exist?");
                        }
                    }

                    if (fileArray.Length > 0)
                    {
                        for (int i = 0;i<fileArray.Length;i++)
                        {
                            string[] jaja = fileArray[i].Split(" ".ToCharArray()[0]);
                            if (jaja.Length == 6)
                            {
                                losOverlaysData = losOverlaysData.AddToArray(jaja);
                                losOverlaysData[losOverlaysData.Length - 1] = losOverlaysData[losOverlaysData.Length - 1].AddToArray(boogeru);
                                losOverlaysData[losOverlaysData.Length - 1] = losOverlaysData[losOverlaysData.Length - 1].AddToArray(i.ToString());
                            }
                            else
                            {
                                Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in a nonexistent Overlayset. File \"content/text_"+self.manager.rainWorld.inGameTranslator.currentLanguage+"/datingutils/overlayset_"+boogeru+".txt\" line "+i+" has "+jaja.Length+" entries instead of 6.");
                            }
                        }
                        Plugin.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Overlayset \"overlayset_"+boogeru+"\" initialized!");
                    }
                }
                
            }
            
            // Overlay application (for when there is OverlayData but not Overlays)
            if (losOverlaysData.Length > losOverlays.Length)
            {
                for (int i = losOverlays.Length; i < losOverlaysData.Length; i++)
                {
                    try
                    {
                        losOverlays = losOverlays.AddToArray(new MenuIllustration(self, self.scene, "Content", losOverlaysData[i][3], new Vector2(self.manager.rainWorld.options.ScreenSize.x / 7f * 2f - 50f + int.Parse(losOverlaysData[i][4]), self.manager.rainWorld.options.ScreenSize.y * 0.7f + int.Parse(losOverlaysData[i][5])), true, true));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "[NonIdiot's DatingUtils] EXCEPTION! Seems that file \"content/text_" +  self.manager.rainWorld.inGameTranslator.currentLanguage + "/datingutils/overlayset_" + losOverlaysData[i][6] + ".txt\" line " + losOverlaysData[i][7] + " failed. Is the data inputted correctly? The exception says: "+ex);
                    }

                    self.pages[0].subObjects.Add(losOverlays[losOverlays.Length - 1]);
                }
            }

            for (int i=0;i<losOverlaysData.Length;i++)
            {
                try
                {
                    bool abettttt = allVarReturn(losOverlaysData[i][0]) == int.Parse(losOverlaysData[i][1]) && self.slugcat.fileName == (losOverlaysData[i][2].Contains("_anim_") ? losOverlaysData[i][2].Split("_".ToCharArray())[0] : losOverlaysData[i][2]);
                    losOverlays[i].pos = new Vector2(self.manager.rainWorld.options.ScreenSize.x / 7f * 2f - 50f + int.Parse(losOverlaysData[i][4]), self.manager.rainWorld.options.ScreenSize.y * 0.7f + int.Parse(losOverlaysData[i][5]) + (abettttt ? 0 : 9999));
                    if (abettttt)
                    {
                        Logger.Log(LogLevel.Info,
                            "[NonIdiot's DatingUtils] Applied overlay \"content/text_" + self.manager.rainWorld.inGameTranslator.currentLanguage + "/datingutils/overlayset_" + losOverlaysData[i][6] + ".txt\" line " + losOverlaysData[i][7]);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, "[NonIdiot's DatingUtils] EXCEPTION! Seems that file \"content/text_" +  self.manager.rainWorld.inGameTranslator.currentLanguage + "/datingutils/overlayset_" + losOverlaysData[i][6] + ".txt\" line " + losOverlaysData[i][7] + " failed (in a weird way, to make it worse). Is the data inputted correctly? The exception says: "+ex);
                }
            }

            // Finally, applying the changes
            self.GrafUpdate(0f);
        }

        private void IPressButton(DatingSim.orig_Singal orig, MoreSlugcats.DatingSim self, MenuObject sender, string message)
        {
            if (sender is SimpleButton)
            {
                DatingUtils.GeneralCWT.Data Datta = (sender as SimpleButton).GetCustomData();

                //Logger.Log(LogLevel.Info, Datta.typeOf);
                switch (Datta.typeOf)
                {
                    case 0:
                        allVariables[Datta.whichVar] = Datta.setToWhat;
                        Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Set variable \""+Datta.whichVar+"\" to "+Datta.setToWhat);
                        break;
                    case 1:
                        for (int i = 0; i<Math.Min(Datta.whichVarInVarSet.Length,Datta.setToWhatInVarSet.Length); i++)
                        {
                            //if (allVariables.ContainsKey(Datta.whichVarInVarSet[i]))
                            //{
                                allVariables[Datta.whichVarInVarSet[i]] = Datta.setToWhatInVarSet[i];
                                Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Set variable \""+Datta.whichVarInVarSet[i]+"\" to "+Datta.setToWhatInVarSet[i]);
                            //}
                            //else
                            //{
                            //    Logger.Log(LogLevel.Error, "[NonIdiot's DatingUtils] EXCEPTION! Setting variable \""+Datta.whichVarInVarSet[i]+"\" to "+Datta.setToWhatInVarSet[i]+" failed!");
                            //}
                        }
                        break;
                }

                /*for (int k = 0; k < self.messageButtons.Count; k++)
                {
                    if (message == "OPTION" + k.ToString() && self.optionFiles[k] == "start.txt")
                    {
                        
                    }
                }*/
            }

            orig(self, sender, message);
        }

        private void INewMessage(DatingSim.orig_NewMessage_string_int orig, MoreSlugcats.DatingSim self, string text, int extraLinger)
        {
            orig(self,(text.Contains("]") ? text.Split("]".ToCharArray())[1] : text),extraLinger);
        }

        private void PostModsInitt(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
        }

        private void RainWorldOnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                MachineConnector.SetRegisteredOI(Plugin.MOD_ID, DatingUtilsConfig.Instance);
                On.HUD.HUD.InitSinglePlayerHud += Plugin.HUD_InitSingle;
                On.HUD.HUD.InitMultiplayerHud += Plugin.HUD_InitMulti;
            }
            catch (Exception ex)
            {
                Log("[NonIdiot's DatingUtils] EXCEPTION! "+ex.ToString());
            }
        }

        private static void HUD_InitSingle(On.HUD.HUD.orig_InitSinglePlayerHud orig, global::HUD.HUD self, global::RoomCamera camera)
        {
            orig(self, camera);
            try
            {
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static void HUD_InitMulti(On.HUD.HUD.orig_InitMultiplayerHud orig, global::HUD.HUD self, global::ArenaGameSession session)
        {
            orig(self, session);
            try
            {
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // MARKER: Utils
        private void Log(object text)
        {
            Logger.LogDebug("[NonIdiot's DatingUtils] " + text);
        }
        
        public static string stroin(string input)
        {
            return string.Join("", input.ToUpper().ToCharArray());
        }

        public static bool isBetween(int input, int min, int max)
        {
            return (min < max ? input > min && input < max : input > max && input < min);
        }

        public static float floatConstr(float input, float lowest, float highest)
        {
            return Math.Min(Math.Max(input, lowest), highest);
        }

        public static int intConstr(int input, int lowest, int highest)
        {
            return Math.Min(Math.Max(input, lowest), highest);
        }

        // has a limit of 7 characters just to make sure it doesn't go above 9 :troll:
        public static int stringToNumber(string charArray, int startIndex, string fileName)
        {
            string fullString = "";
            for (var i = startIndex; i < Math.Min(charArray.Length,startIndex+7); i++)
            {
                if (charArray[i].ToString().IsNullOrWhiteSpace() || charArray[i].ToString() == "]")
                {
                    break;
                }

                fullString += charArray[i];
            }

            if (fullString != "")
            {
                //Plugin.Log(LogLevel.Info,fullString+" "+fullString.Length);
                try
                {
                    return int.Parse(fullString);
                }
                catch (Exception ex)
                {
                    Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+fileName+" resulted in an error: "+ex.ToString());
                }
            }
            else
            {
                Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+fileName+" in string \""+charArray+"\" starting at index "+startIndex+" resulted in not finding a usable integer.");
            }

            return -1;
        }

        public static string stringToSmol(string charArray, int startIndex, string fileName)
        {
            string fullString = "";
            for (var i = startIndex; i < charArray.Length; i++)
            {
                if (charArray[i].ToString().IsNullOrWhiteSpace() || charArray[i].ToString() == "]")
                {
                    break;
                }

                fullString += charArray[i];
            }

            if (fullString != "")
            {
                //Plugin.Log(LogLevel.Info,fullString+" "+fullString.Length);
                try
                {
                    return fullString;
                }
                catch (Exception ex)
                {
                    Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+fileName+" resulted in an error: "+ex.ToString());
                }
            }
            else
            {
                Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+fileName+" in string \""+charArray+"\" starting at index "+startIndex+" resulted in not finding a usable name.");
            }
            
            return "-1";
        }

        public int allVarReturn(string whichVar)
        {
            return (allVariables.ContainsKey(whichVar) ? allVariables[whichVar] : 0);
        }
    }

    public class DatingUtilsConfig : OptionInterface
    {
        public static DatingUtilsConfig Instance { get; } = new DatingUtilsConfig();

        public static void RegisterOI()
        {
            if (MachineConnector.GetRegisteredOI(Plugin.MOD_ID) != Instance)
                MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Instance);
        }
        // Menus and stuff
        public override void Initialize()
        {
            base.Initialize();
            Tabs = [
                new OpTab(this, "Main Page")
            ];

            Tabs[0].AddItems([
                new OpLabel(30f, 560f, "NonIdiot's Dating Sim Utils Config - Main Page", true),
            ]);
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
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
            public int minOrMax = 69420;
            public string[] whichVarInVarSet;
            public int[] setToWhatInVarSet;
            public int[] minOrMaxInVarSet;
            public int[] commandIsWhatInVarSet;
            public bool dead = false;
        }
    }

    [BepInPlugin(MOD_ID, "NonIdiot's Dating Sim Utils", "1.0.5")]
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
        public bool shouldILetGrafUpdateWork = true;
        public bool shouldAlsoLetUpdateWork = false;
        
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
                    On.MoreSlugcats.DatingSim.GrafUpdate += ThouShaltNotUpdate;
                    On.MoreSlugcats.DatingSim.Update += ThouShallUpdate;
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

            shouldILetGrafUpdateWork = false;
            shouldAlsoLetUpdateWork = false;
            orig(self, filename);
            
            if (filename == "start.txt")
            {
                allVariables = new Dictionary<string, int>();
                Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Set all variables to a default of 0");
                if (!DatingUtilsConfig.examplePathOn.Value)
                {
                    for (int i = 0; i<self.messageButtons.Count;i++)
                    {
                        if (stroin(self.messageButtons[i].menuLabel.text) == stroin("To The Pants Store"))
                        {
                            Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Hiding button \""+self.messageButtons[i].menuLabel.text+"\" because the configuration option for showing it is False");
                            self.messageButtons[i].RemoveSprites();
                            self.pages[0].selectables.Remove(self.messageButtons[i]);
                            self.pages[0].subObjects.Remove(self.messageButtons[i]);
                            self.messageButtons[i].GetCustomData().dead = true;
                        }
                    }
                }
            }
            
            if (!self.creditsLabelLeft.text.Contains("Additional functionality added by NonIdiot"))
            {
                self.creditsLabelLeft.text += Environment.NewLine+"Additional functionality added by NonIdiot";
            }
            
            // Variable checking, setting, etc.
            for (int i = 0; i < self.messageButtons.Count; i++)
            {
                char[] myArray = self.messageButtons[i].menuLabel.text.ToCharArray();
                bool[] isWorking = [true,true,true,true,true];
                int[] sset = [5,8,7,10,8];
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
                    if (j <= 6 && myArray[j] != "[varadd".ToCharArray()[j])
                    {
                        isWorking[4] = false;
                    }

                    if (isWorking == new[] {false, false, false, false, false})
                    {
                        Log("im not doing moss " + self.messageButtons[i].menuLabel.text);
                        break;
                    }
                }

                if (!self.messageButtons[i].GetCustomData().dead && isWorking != new[] {false, false, false, false, false} && self.messageButtons[i].menuLabel.text.ToCharArray()[0].ToString() == "[")
                {
                    int numm = -1;
                    // note to self: add a number to the "for (var j = #" every time you add a new command
                    for (var j = 4; j > -1; j--)
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
                    if (numm == 0 || numm == 2 || numm == 4)
                    {
                        //Logger.Log(LogLevel.Info, "jug of mug "+numm+" "+sset[numm] + " " + self.messageButtons[i].GetCustomData().whichVar + " " + self.messageButtons[i].GetCustomData().whichVar.Length);
                        self.messageButtons[i].GetCustomData().setToWhat = stringToNumber(string.Join("", myArray).Trim(), sset[numm] + self.messageButtons[i].GetCustomData().whichVar.Length + 1, filename,1);
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
                        if (numm == 4 && string.Join("", myArray).Trim()[sset[numm] + self.messageButtons[i].GetCustomData().whichVar.Length + self.messageButtons[i].GetCustomData().setToWhat.ToString().Length + 1].ToString() != "]")
                        {
                            self.messageButtons[i].GetCustomData().minOrMax = stringToNumber(string.Join("", myArray).Trim(), sset[numm] + self.messageButtons[i].GetCustomData().whichVar.Length + self.messageButtons[i].GetCustomData().setToWhat.ToString().Length + 2, filename,2);
                        }
                        else
                        {
                            self.messageButtons[i].GetCustomData().minOrMax = 69420;
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
                                for (int filee = 0;filee< fileArray.Length;filee++)
                                {
                                    string[] jolan = fileArray[filee].Trim().Split(" ".ToCharArray());
                                    if (jolan.Length > 1 && jolan[0] != "//")
                                    {
                                        if (jolan.Length == 2)
                                        {
                                            string returnString = stringToSmol(jolan[0], 0, filename);
                                            int returnNum = stringToNumber(jolan[1], 0, filename,3);
                                            self.messageButtons[i].GetCustomData().whichVarInVarSet = self.messageButtons[i].GetCustomData().whichVarInVarSet.AddItem(returnString).ToArray();
                                            self.messageButtons[i].GetCustomData().setToWhatInVarSet = self.messageButtons[i].GetCustomData().setToWhatInVarSet.AddItem(returnNum).ToArray();
                                            self.messageButtons[i].GetCustomData().commandIsWhatInVarSet = self.messageButtons[i].GetCustomData().commandIsWhatInVarSet.AddItem(0).ToArray();
                                        }
                                        else if (jolan.Length == 3 || jolan.Length == 4)
                                        {
                                            if (jolan[0] == "varadd")
                                            {
                                                string returnString = stringToSmol(jolan[1], 0, filename);
                                                int returnNum = stringToNumber(jolan[2], 0, filename,4);
                                                self.messageButtons[i].GetCustomData().whichVarInVarSet = self.messageButtons[i].GetCustomData().whichVarInVarSet.AddItem(returnString).ToArray();
                                                self.messageButtons[i].GetCustomData().setToWhatInVarSet = self.messageButtons[i].GetCustomData().setToWhatInVarSet.AddItem(returnNum).ToArray();
                                                if (jolan.Length == 4)
                                                {
                                                    self.messageButtons[i].GetCustomData().minOrMaxInVarSet = self.messageButtons[i].GetCustomData().commandIsWhatInVarSet.AddItem(stringToNumber(jolan[3], 0, filename,5)).ToArray();
                                                }
                                                else
                                                {
                                                    self.messageButtons[i].GetCustomData().minOrMaxInVarSet = self.messageButtons[i].GetCustomData().commandIsWhatInVarSet.AddItem(69420).ToArray();
                                                }
                                                self.messageButtons[i].GetCustomData().commandIsWhatInVarSet = self.messageButtons[i].GetCustomData().commandIsWhatInVarSet.AddItem(1).ToArray();
                                            }
                                            else
                                            {
                                                Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in an invalid line in the Varset. File \"content/text_"+self.manager.rainWorld.inGameTranslator.currentLanguage+"/datingutils/varset_"+self.messageButtons[i].GetCustomData().whichVar+".txt\" line "+filee+" has 3/4 entries, but the first one is not \"varadd\".");
                                            }
                                        }
                                        else
                                        {
                                            Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in an invalid line in the Varset. File \"content/text_"+self.manager.rainWorld.inGameTranslator.currentLanguage+"/datingutils/varset_"+self.messageButtons[i].GetCustomData().whichVar+".txt\" line "+filee+" has "+jolan.Length+" entries instead of 2 or 3.");
                                        }
                                    }
                                }
                            }
                        }
                        else if (numm == 3)
                        {
                            self.messageButtons[i].GetCustomData().setToWhat = stringToNumber(string.Join("", myArray).Trim(), sset[numm] + self.messageButtons[i].GetCustomData().whichVar.Length + 1, filename,6);
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
                                    if (stroin(returnString) != stroin("//"))
                                    {
                                        int returnNum = stringToNumber(filee, returnString.Length+1, filename, 7);
                                        //Logger.Log(LogLevel.Info, "AAAAA "+returnString+" "+returnNum);
                                        self.messageButtons[i].GetCustomData().whichVarInVarSet = self.messageButtons[i].GetCustomData().whichVarInVarSet.AddItem(returnString).ToArray();
                                        self.messageButtons[i].GetCustomData().setToWhatInVarSet = self.messageButtons[i].GetCustomData().setToWhatInVarSet.AddItem(returnNum).ToArray();
                                    }
                                }

                                //Logger.Log(LogLevel.Info, "AAAAE "+self.messageButtons[i].GetCustomData().whichVarInVarSet.Length);
                                for (int j = 0; j<self.messageButtons[i].GetCustomData().whichVarInVarSet.Length; j++)
                                {
                                    int aBul = allVarReturn(self.messageButtons[i].GetCustomData().whichVarInVarSet[j]);
                                    bool laBool = aBul == self.messageButtons[i].GetCustomData().setToWhatInVarSet[j];
                                    Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Checkset \""+self.messageButtons[i].menuLabel.text+"\" check #"+j+" (var \""+self.messageButtons[i].GetCustomData().whichVarInVarSet[j]+"\"=="+self.messageButtons[i].GetCustomData().setToWhatInVarSet[j]+") came back with "+laBool+" (var returned "+aBul+")");
                                    if (!laBool && self.messageButtons[i].GetCustomData().setToWhat != 1)
                                    {
                                        // AND checkset
                                        begoneThee = true;
                                        break;
                                    }
                                    if (laBool && self.messageButtons[i].GetCustomData().setToWhat == 1)
                                    {
                                        // OR checkset
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
            /*
            int[] prenum4 = [];
            float prenum8 = 0f;
            for (int prenum9 = 0; prenum9 < self.messageButtons.Count; prenum9++)
            {
                if (!self.messageButtons[prenum9].GetCustomData().dead)
                {
                    prenum8 += self.messageButtons[prenum9].size.x + 10f;
                    if (prenum9 + 1 < self.messageButtons.Count && prenum8 + self.messageButtons[prenum9].size.x + 30 > self.manager.rainWorld.options.ScreenSize.x/2)
                    {
                        prenum4 = prenum4.AddToArray(prenum9);
                    }
                }
            }
            */
            float num8 = 0f;
            for (int num9 = 0; num9 < self.messageButtons.Count; num9++)
            {
                if (!self.messageButtons[num9].GetCustomData().dead)
                {
                    /*
                    for (int num10 = 0; num10 < prenum4.Length; num10++)
                    {
                        if (prenum4[num10] == num9 - 1)
                        {

                        }
                    }
                    */
                    self.messageButtons[num9].pos.x = self.manager.rainWorld.options.ScreenSize.x * 0.5f - num5 * 0.5f +
                                                      num8 + (1366f - self.manager.rainWorld.options.ScreenSize.x) / 2f;
                    num8 += self.messageButtons[num9].size.x + 10f;
                }
            }
            
            // File preloading for all Overlay-related stuff
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

            // Expanded Dating Simulator Engine compat
            int whereOverlayLine = -1;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == "" && i+1<array.Length)
                {
                    whereOverlayLine = i+1;
                    break;
                }
            }

            // Overlay nonsense (aka setup of overlay data)
            bool bonanar = true;
            string woga = "[overlayset ";
            if (whereOverlayLine == -1 || woga.Length+2 > array[whereOverlayLine].Length)
            {
                bonanar = false;
            }
            else
            {
                for (int i=0;i<woga.Length;i++)
                {
                    if (array[whereOverlayLine][i] != woga[i])
                    {
                        bonanar = false;
                        break;
                    }
                }
            }
            if (bonanar)
            {
                if (array[whereOverlayLine][12].ToString() == "-" && array[whereOverlayLine][13].ToString() == "1")
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
                    string boogeru = stringToSmol(array[whereOverlayLine], 12, filename);
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
                            if (stroin(jaja[0]) != "" && stroin(jaja[0]) != stroin("//"))
                            {
                                if (jaja.Length == 6)
                                {
                                    losOverlaysData = losOverlaysData.AddToArray(jaja);
                                    losOverlaysData[losOverlaysData.Length - 1] = losOverlaysData[losOverlaysData.Length - 1].AddToArray(boogeru);
                                    losOverlaysData[losOverlaysData.Length - 1] = losOverlaysData[losOverlaysData.Length - 1].AddToArray(i.ToString());
                                    //Plugin.Log(LogLevel.Info, "ahe "+losOverlaysData[losOverlaysData.Length - 1]);
                                    //Plugin.Log(LogLevel.Info, "a overlayset_" + boogeru + " line "+i+" is length "+losOverlaysData.Length);
                                }
                                else if (jaja.Length == 2 && losOverlaysData.Length != 0)
                                {
                                    losOverlaysData[losOverlaysData.Length - 1] = losOverlaysData[losOverlaysData.Length - 1].AddToArray(jaja[0]);
                                    losOverlaysData[losOverlaysData.Length - 1] = losOverlaysData[losOverlaysData.Length - 1].AddToArray(jaja[1]);
                                    //Plugin.Log(LogLevel.Info, "b overlayset_" + boogeru + " line "+i+" is length "+losOverlaysData.Length);
                                }
                                else
                                {
                                    Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+filename+" resulted in a nonexistent Overlayset. File \"content/text_"+self.manager.rainWorld.inGameTranslator.currentLanguage+"/datingutils/overlayset_"+boogeru+".txt\" line "+i+" has "+jaja.Length+" entries instead of 6 or 2.");
                                }
                            }
                            else
                            {
                                //Plugin.Log(LogLevel.Info, "c overlayset_" + boogeru + " line "+i+" is length "+losOverlaysData.Length);
                            }
                        }
                        Plugin.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Overlayset \"overlayset_"+boogeru+"\" initialized!");
                    }
                }
                
            }

            // Overlay data application (for when there is OverlayData but not Overlays)
            if (losOverlaysData.Length > losOverlays.Length)
            {
                for (int i = losOverlays.Length; i < losOverlaysData.Length; i++)
                {
                    try
                    {
                        losOverlays = losOverlays.AddToArray(new MenuIllustration(self, self.scene, "Content", losOverlaysData[i][3], new Vector2(self.manager.rainWorld.options.ScreenSize.x / 7f * 2f - 50f + int.Parse(losOverlaysData[i][4]), 9999 + int.Parse(losOverlaysData[i][5])), true, true));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "[NonIdiot's DatingUtils] EXCEPTION! Seems that file \"content/text_" +  self.manager.rainWorld.inGameTranslator.currentLanguage + "/datingutils/overlayset_" + losOverlaysData[i][6] + ".txt\" line " + losOverlaysData[i][7] + " failed. Is the data inputted correctly? The exception says: "+ex);
                    }

                    self.pages[0].subObjects.Add(losOverlays[losOverlays.Length - 1]);
                }
            }
            
            // Line Check stuffamajigs
            string[] woga2 = ["[linecheck ","[linecheckset "];
            bool[] bonanar2 = [true, true];
            int moveItBack = 0;
            if (whereOverlayLine != -1)
            {
                for (int i = whereOverlayLine + 1; i < array.Length; i++)
                {
                    if (array[i].Length <= 1)
                    {
                        break;
                    }
                    if (woga2[0].Length + 2 <= array[i].Length)
                    {
                        bonanar2 = [true, true];
                        for (int k = 0; k < woga2.Length; k++)
                        {
                            for (int j=0;j<Math.Min(array[i].Length,woga2[k].Length);j++)
                            {
                                if (array[i][j] != woga2[k][j])
                                {
                                    bonanar2[k] = false;
                                    break;
                                }
                            }
                        }
                        if (bonanar2[1] || bonanar2[0])
                        {
                            bool myeBool = true;
                            if (bonanar2[1])
                            {
                                // for Linechecksets
                                //Logger.Log(LogLevel.Info, "linecheckset 1");
                                string returnString2 = stringToSmol(array[i], 14, filename);
                                int returnNumber2 = (stringToSmol(array[i], 14+returnString2.Length, filename).StartsWith("[") ? 0 : stringToNumber(array[i], 14+returnString2.Length+1, filename, 10));
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
                                    returnString2,
                                    ".txt",
                                }));
                                //Logger.Log(LogLevel.Info, "linecheckset 2");
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
                                        returnString2,
                                        ".txt",
                                    }));
                                    if (File.Exists(path))
                                    {
                                        //Logger.Log(LogLevel.Info, "linecheckset 2");
                                        fileArray = File.ReadAllLines(path);
                                    }
                                    else
                                    {
                                        Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading a Linecheckset in file "+filename+" resulted in a nonexistent Checkset. Does file \"content/text_"+LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English)+"/datingutils/checkset_"+returnString2+".txt\" exist?");
                                    }
                                }
                                
                                if (fileArray.Length > 0)
                                {
                                    //Logger.Log(LogLevel.Info, "linecheckset 3");
                                    int whichCheckNum = -1;
                                    foreach (string filee in fileArray)
                                    {
                                        string returnString = stringToSmol(filee, 0, filename);
                                        //Logger.Log(LogLevel.Info, "linecheckset 4");
                                        if (stroin(returnString) != stroin("//"))
                                        {
                                            whichCheckNum++;
                                            int returnNum = stringToNumber(filee, returnString.Length+1, filename, 9);
                                            //Logger.Log(LogLevel.Info, "AAAAA "+returnString+" "+returnNum);
                                            int aBul = allVarReturn(returnString);
                                            bool laBool = (aBul == returnNum);
                                            Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Checkset \""+array[i]+"\" check #"+whichCheckNum+" (var \""+returnString+"\"=="+returnNum+") for a Linecheckset came back with "+laBool+" (var returned "+aBul+")");
                                            if (!laBool && returnNumber2 != 1)
                                            {
                                                // AND checkset
                                                myeBool = false;
                                                break;
                                            }
                                            if (laBool && returnNumber2 == 1)
                                            {
                                                // OR checkset
                                                myeBool = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // for Linechecks
                                string returnString2 = stringToSmol(array[i], 11, filename);
                                int returnNumber2 = stringToNumber(array[i], 11+returnString2.Length+1, filename, 8);
                                //Logger.Log(LogLevel.Info, "return is "+returnString2+" num is "+returnNumber2);
                                myeBool = allVarReturn(returnString2) == returnNumber2;
                                Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Linecheck \""+array[i]+"\" (var \""+returnString2+"\"=="+returnNumber2+") came back with "+myeBool+" (var returned "+allVarReturn(returnString2)+")");
                            }
                            if (!myeBool)
                            {
                                int iiReal = i - whereOverlayLine - moveItBack;// + 1
                                //Logger.Log(LogLevel.Info,"norb "+(iiReal)+" "+self.messageLabels.Count);
                                /*
                                for (int j = 0; j < Math.Min(iiReal,self.messageLabels.Count); j++)
                                {
                                    self.messageLabels[j].text += " aaaaa";
                                    if (self.messageLabels[j].pos.y < -999f)
                                    {
                                        //self.messageLabels[j].pos.y = self.manager.rainWorld.screenSize.y * 0.25f + self.totalHeight * 0.5f - MoreSlugcats.DatingSim.lineHeight * 0.6666f;
                                    }
                                    self.messageLabels[j].pos.y -= MoreSlugcats.DatingSim.lineHeight * 0.6666f;
                                    self.messageLabels[j].GrafUpdate(0f);
                                    self.messageLabels[j].Update();
                                    self.messageLabels[j].label.Redraw(true,true);
                                    Logger.Log(LogLevel.Info,self.messageLabels[j].pos.y+" "+j+" 1b");
                                    //Logger.Log(LogLevel.Info,self.messageLabels[j].ScreenPos.y+" "+j+" 2b");
                                    //Logger.Log(LogLevel.Info,self.messageLabels[j].label.y+" "+j+" 3b");
                                }*/
                                //Logger.Log(LogLevel.Info,self.messageLabels.Count+" "+Math.Min(iiReal,self.messageLabels.Count-1)+" 5b");//self.messageLabels[Math.Min(iiReal,self.messageLabels.Count-1)].pos.y+
                                self.messages.RemoveAt(Math.Min(iiReal,self.messages.Count-1));
                                self.messageLabels.RemoveAt(Math.Min(iiReal,self.messageLabels.Count-1));
                                //self.messageLabels[Math.Min(iiReal,self.messageLabels.Count-1)].GrafUpdate(0f);
                                //self.messageLabels[Math.Min(iiReal,self.messageLabels.Count-1)].Update();
                                //self.messageLabels[Math.Min(iiReal,self.messageLabels.Count-1)].label.Redraw(true,true);
                                for (int j = iiReal; j < self.messageWidths.Length - 1; j++)
                                {
                                    self.messageWidths[j] = self.messageWidths[j + 1];
                                }
                                moveItBack++;
                            }
                        }
                    }
                }
                (self as Menu.Menu).GrafUpdate(0f);
            }

            // Finally, applying the changes
            //Logger.Log(LogLevel.Info, "ua");
            for (int j = 0; j < self.messageLabels.Count; j++)
            {
                //Logger.Log(LogLevel.Info,self.messageLabels[j].pos.y+" "+j+" 1z");
                //Logger.Log(LogLevel.Info,self.messageLabels[j].ScreenPos.y+" "+j+" 2z");
                //Logger.Log(LogLevel.Info,self.messageLabels[j].label.y+" "+j+" 3z");
                //Logger.Log(LogLevel.Info,self.messageLabels[j].DrawPos(0f).y+" "+j+" 4z");
            }
            // General overlay application (showing the overlays)
            shouldAlsoLetUpdateWork = true;
            shouldILetGrafUpdateWork = true;
            self.GrafUpdate(0f);
            for (int j = 0; j < self.messageLabels.Count; j++)
            {
                //Logger.Log(LogLevel.Info,self.messageLabels[j].pos.y+" "+j+" 1a");
                //Logger.Log(LogLevel.Info,self.messageLabels[j].ScreenPos.y+" "+j+" 2a");
                //Logger.Log(LogLevel.Info,self.messageLabels[j].label.y+" "+j+" 3a");
                //Logger.Log(LogLevel.Info,self.messageLabels[j].DrawPos(0f).y+" "+j+" 4a");
            }
            //Logger.Log(LogLevel.Info, "au");
        }

        private void IPressButton(DatingSim.orig_Singal orig, MoreSlugcats.DatingSim self, MenuObject sender, string message)
        {
            if (sender is SimpleButton)
            {
                DatingUtils.GeneralCWT.Data Datta = (sender as SimpleButton).GetCustomData();

                //Logger.Log(LogLevel.Info, Datta.typeOf);
                switch (Datta.typeOf)
                {
                    // Runs when a button with a var command is pressed
                    case 0:
                    {
                        allVariables[Datta.whichVar] = Datta.setToWhat;
                        Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Set variable \""+Datta.whichVar+"\" to "+Datta.setToWhat);
                        break;
                    }
                    // Runs when a button with a varset command is pressed
                    case 1:
                    {
                        for (int i = 0; i<Math.Min(Datta.whichVarInVarSet.Length,Math.Min(Datta.setToWhatInVarSet.Length,Datta.commandIsWhatInVarSet.Length)); i++)
                        {
                            switch (Datta.commandIsWhatInVarSet[i])
                            {
                                case 0:
                                {
                                    allVariables[Datta.whichVarInVarSet[i]] = Datta.setToWhatInVarSet[i];
                                    Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Set variable \""+Datta.whichVarInVarSet[i]+"\" to "+Datta.setToWhatInVarSet[i]);
                                    break;
                                }
                                case 1:
                                {
                                    if (Datta.minOrMaxInVarSet[i] == 69420)
                                    {
                                        if (allVariables.ContainsKey(Datta.whichVarInVarSet[i]))
                                        {
                                            allVariables[Datta.whichVarInVarSet[i]] += Datta.setToWhatInVarSet[i];
                                        }
                                        else
                                        {
                                            allVariables[Datta.whichVarInVarSet[i]] = Datta.setToWhatInVarSet[i];
                                        }
                                        Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Added variable \""+Datta.whichVarInVarSet[i]+"\" with "+Datta.setToWhatInVarSet[i]+" to make it become "+allVariables[Datta.whichVarInVarSet[i]]);
                                    }
                                    else
                                    {
                                        if (allVariables.ContainsKey(Datta.whichVarInVarSet[i]))
                                        {
                                            allVariables[Datta.whichVarInVarSet[i]] = (Datta.minOrMaxInVarSet[i] > 0 ? Math.Min(allVariables[Datta.whichVarInVarSet[i]]+Datta.setToWhatInVarSet[i],Datta.minOrMaxInVarSet[i]) : Math.Max(allVariables[Datta.whichVarInVarSet[i]]+Datta.setToWhatInVarSet[i],Datta.minOrMaxInVarSet[i]));
                                        }
                                        else
                                        {
                                            allVariables[Datta.whichVarInVarSet[i]] = (Datta.minOrMaxInVarSet[i] > 0 ? Math.Min(Datta.setToWhatInVarSet[i],Datta.minOrMaxInVarSet[i]) : Math.Max(Datta.setToWhatInVarSet[i],Datta.minOrMaxInVarSet[i]));
                                        }
                                        Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Added variable \""+Datta.whichVarInVarSet[i]+"\" with "+Datta.setToWhatInVarSet[i]+" (limited to a "+(Datta.minOrMaxInVarSet[i] > 0 ? "max":"min")+" of "+Datta.minOrMaxInVarSet[i]+") to make it become "+allVariables[Datta.whichVarInVarSet[i]]);
                                    }
                                    break;
                                }
                                default:
                                {
                                    Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Somehow, reading a Varset on the button that was last pressed resulted in a command index of "+Datta.commandIsWhatInVarSet[i]+", which doesn't exist.");
                                    break;
                                }
                                //}
                                //else
                                //{
                                //    Logger.Log(LogLevel.Error, "[NonIdiot's DatingUtils] EXCEPTION! Setting variable \""+Datta.whichVarInVarSet[i]+"\" to "+Datta.setToWhatInVarSet[i]+" failed!");
                                //}
                            }
                        }
                        break;
                    }
                    // Runs when a button with a varadd command is pressed
                    case 4:
                    {
                        if (Datta.minOrMax == 69420)
                        {
                            if (allVariables.ContainsKey(Datta.whichVar))
                            {
                                allVariables[Datta.whichVar] += Datta.setToWhat;
                            }
                            else
                            {
                                allVariables[Datta.whichVar] = Datta.setToWhat;
                            }
                            Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Added variable \""+Datta.whichVar+"\" with "+Datta.setToWhat+" to make it become "+allVariables[Datta.whichVar]);
                        }
                        else
                        {
                            // was gonna do something where if you did a negative number, it would constrain between -|number| and |number|,
                            // and if you did a positive number, it would constrain out of that range, but it was too unclear so i scrapped it
                            if (allVariables.ContainsKey(Datta.whichVar))
                            {
                                allVariables[Datta.whichVar] = (Datta.minOrMax > 0 ? Math.Min(allVariables[Datta.whichVar]+Datta.setToWhat,Datta.minOrMax) : Math.Max(allVariables[Datta.whichVar]+Datta.setToWhat,Datta.minOrMax));
                            }
                            else
                            {
                                allVariables[Datta.whichVar] = (Datta.minOrMax > 0 ? Math.Min(Datta.setToWhat,Datta.minOrMax) : Math.Max(Datta.setToWhat,Datta.minOrMax));
                            }
                            Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Added variable \""+Datta.whichVar+"\" with "+Datta.setToWhat+" (limited to a "+(Datta.minOrMax > 0 ? "max":"min")+" of "+Datta.minOrMax+") to make it become "+allVariables[Datta.whichVar]);
                        }
                        break;
                    }
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

        private void ThouShaltNotUpdate(DatingSim.orig_GrafUpdate orig, MoreSlugcats.DatingSim self, float timeStacker)
        {
            if (shouldILetGrafUpdateWork)
            {
                orig(self, timeStacker);
            }

            //shouldILetGrafUpdateWork = true;
        }

        private void ThouShallUpdate(DatingSim.orig_Update orig, MoreSlugcats.DatingSim self)
        {
            if (shouldAlsoLetUpdateWork)
            {
                for (int i=0;i<losOverlaysData.Length;i++)
                {
                    try
                    {
                        //Logger.Log(LogLevel.Info, "awawag " + losOverlaysData[i][2]+" "+losOverlaysData[i].Length);
                        bool abettttt = allVarReturn(losOverlaysData[i][0]) == int.Parse(losOverlaysData[i][1]) && self.slugcat.fileName == (losOverlaysData[i][2].Contains("_anim_") ? losOverlaysData[i][2].Split("_".ToCharArray())[0] : losOverlaysData[i][2]);
                        if (abettttt)
                        {
                            //Logger.Log(LogLevel.Info, "ajinito "+abettttt+" ggj "+losOverlaysData[i].Length+" "+(losOverlaysData[i].Length % 2));
                        }

                        if (abettttt && losOverlaysData[i].Length > 8 && losOverlaysData[i].Length % 2 == 0)
                        {
                            for (var j = 8; j+1 < losOverlaysData[i].Length; j += 2)
                            {
                                bool abeeette = allVarReturn(losOverlaysData[i][j]) == int.Parse(losOverlaysData[i][j + 1]);
                                //Logger.Log(LogLevel.Info, "aeienre "+j+" aa "+abeeette);
                                if (!abeeette)
                                {
                                    abettttt = false;
                                    break;
                                }
                            }
                        }
                        losOverlays[i].pos = new Vector2(self.manager.rainWorld.options.ScreenSize.x / 7f * 2f - 50f + int.Parse(losOverlaysData[i][4]), (abettttt ? self.manager.rainWorld.options.ScreenSize.y * 0.7f + int.Parse(losOverlaysData[i][5]) : 2000));
                        if (abettttt)
                        {
                            Logger.Log(LogLevel.Info, "[NonIdiot's DatingUtils] Applied overlay \"content/text_" + self.manager.rainWorld.inGameTranslator.currentLanguage + "/datingutils/overlayset_" + losOverlaysData[i][6] + ".txt\" line " + losOverlaysData[i][7]);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, "[NonIdiot's DatingUtils] EXCEPTION! Seems that file \"content/text_" +  self.manager.rainWorld.inGameTranslator.currentLanguage + "/datingutils/overlayset_" + losOverlaysData[i][6] + ".txt\" line " + losOverlaysData[i][7] + " failed (in a weird way, to make it worse). Is the data inputted correctly? The exception says: "+ex);
                    }
                }
                shouldAlsoLetUpdateWork = false;
            }

            orig(self);
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
        public static int stringToNumber(string charArray, int startIndex, string fileName, int errorCode)
        {
            string fullString = "";
            for (var i = startIndex; i < Math.Min(charArray.Length,startIndex+7); i++)
            {
                //Plugin.Log(LogLevel.Info, "aunt "+i+" "+startIndex);
                if ((i>startIndex && charArray[i].ToString().IsNullOrWhiteSpace()) || charArray[i].ToString() == "]")
                {
                    break;
                }
                if (!charArray[i].ToString().IsNullOrWhiteSpace())
                {
                    fullString += charArray[i];
                }
            }

            if (!fullString.IsNullOrWhiteSpace())
            {
                //Plugin.Log(LogLevel.Info,fullString+" "+fullString.Length);
                try
                {
                    return int.Parse(fullString);
                }
                catch (Exception ex)
                {
                    Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+fileName+" on string \""+charArray+"\" at index "+startIndex+" (which returned "+fullString+") resulted in an error with ErrorCode 00"+errorCode+": "+ex.ToString());
                }
            }
            else
            {
                Plugin.Log(LogLevel.Error,"[NonIdiot's DatingUtils] EXCEPTION! Reading file "+fileName+" in string \""+charArray+"\" starting at index "+startIndex+" resulted in not finding a usable integer. ErrorCode 01"+errorCode);
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
        
        public static Configurable<bool> examplePathOn = Instance.config.Bind("examplePathOn", false,
            new ConfigurableInfo("Whether the pants-related path (made as an example for mod devs) is enabled.  Default false.")
        );
        
        // Menus and stuff
        public override void Initialize()
        {
            base.Initialize();
            Tabs = [
                new OpTab(this, "Main Page")
            ];

            Tabs[0].AddItems([
                new OpLabel(30f, 560f, "NonIdiot's Dating Sim Utils Config - Main Page", true),
                new OpCheckBox(examplePathOn, new Vector2(30f, 500f)) { description = examplePathOn.info.description },
                new OpLabel(60f, 500f, "Example Path Enabled"),
                new OpLabel(30f, 450f, "...there's nothing else to configure here, sorry"),
            ]);
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
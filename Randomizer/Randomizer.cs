using MSCLoader;
using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Linq;

namespace Randomizer
{
    public class Randomizer : Mod
    {
        public override string ID => "Randomizer"; // Your (unique) mod ID 
        public override string Name => "Randomizer"; // Your mod name
        public override string Author => "Bitpro17"; // Name of the Author (your name)
        public override string Version => "1.0"; // Version
        public override string Description => ""; // Short description of your mod

        public override void ModSetup()
        {
            SetupFunction(Setup.PostLoad, Mod_PostLoad);
            SetupFunction(Setup.ModSettings, Mod_Settings);
            SetupFunction(Setup.Update, Mod_Update);
        }

        Keybind scrambleBind;
        private void Mod_Settings()
        {
            scrambleBind = Keybind.Add(this, "scrambleBind", "Randomize", KeyCode.R, KeyCode.LeftControl);
        }

        private void Mod_PostLoad()
        {
            GetSubtitleVariables();
            GetStats();
            //Randomize();
        }

        bool eee;
        void Mod_Update()
        {
            if (scrambleBind.GetKeybindDown())
                eee = true;

            if (eee)
                Randomize();
        }

        FsmString[] subtitleVars;
        string[] subtitleStrings;
        void GetSubtitleVariables()
        {
            FsmString guiSubtitle = FsmVariables.GlobalVariables.GetFsmString("GUIsubtitle");

            List<FsmString> subtitles = new List<FsmString>();
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                PlayMakerFSM[] fsms = t.GetComponents<PlayMakerFSM>();
                foreach (PlayMakerFSM fsm in fsms)
                {
                    if (fsm.FsmName != "SetText")
                    {
                        fsm.Fsm.Init(fsm);
                        foreach (FsmState state in fsm.FsmStates)
                        {
                            foreach (FsmStateAction action in state.Actions)
                            {
                                if (action is SetStringValue)
                                {
                                    SetStringValue setStringValue = (SetStringValue)action;
                                    if (setStringValue.stringVariable == guiSubtitle)
                                        subtitles.Add(setStringValue.stringValue);
                                }
                            }

                        }
                    }
                }
            }
            subtitleVars = subtitles.ToArray();

            subtitleStrings = new string[subtitleVars.Length];
            for (int i = 0; i < subtitleStrings.Length; i++)
                subtitleStrings[i] = subtitleVars[i].Value;
        }

        Transform[] stats;
        Vector3 startPos;
        string[] statStrings;
        void GetStats()
        {
            stats = new Transform[6];
            Transform hud = GameObject.Find("HUD").transform;
            for (int i = 0; i < stats.Length; i++)
                stats[i] = hud.GetChild(i + 2);

            startPos = stats[stats.Length - 1].position;

            List<string> strings = new List<string>();
            foreach (Transform t in stats)
            {
                strings.Add(t.FindChild("HUDLabel").GetComponent<TextMesh>().text);
            }
            statStrings = strings.ToArray();
        }

        Color GetRandomColor()
        {
            return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
        }

        public void Randomize()
        {
            List<Material> mats = new List<Material>();
            List<Renderer> worldRenderers = new List<Renderer>();
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (!(renderer.material.shader.name == "GUI/Text Shader"))
                {
                    mats.Add(renderer.material);
                    //if (!(renderer.material.shader.name == "Windshield/windshield"))
                        worldRenderers.Add(renderer);
                }
            }
            foreach (Renderer worldRenderer in worldRenderers)
            {
                int rand = Random.Range(0, mats.Count);
                worldRenderer.material = mats[rand];
                mats.RemoveAt(rand);
            }

            foreach (Light light in Resources.FindObjectsOfTypeAll<Light>())
            {
                light.color = GetRandomColor();
            }

            List<string> subtitles = new List<string>(subtitleStrings);
            foreach (FsmString strVar in subtitleVars)
            {
                int rand = Random.Range(0, subtitles.Count);
                strVar.Value = subtitles[rand];
                subtitles.RemoveAt(rand);
            }

            List<Transform> statDisplays = new List<Transform>(stats);
            List<string> statDisplayStrings = new List<string>(statStrings);
            int statIndex = 0;
            for (int i = 0; i < stats.Length; i++)
            {
                int rand = Random.Range(0, statDisplays.Count);
                Transform t = statDisplays[rand];
                statDisplays.RemoveAt(rand);

                t.localPosition = new Vector3(-11.5f, 7.2f + 0.4f * statIndex);

                int rand2 = Random.Range(0, statDisplayStrings.Count);
                Transform text = t.FindChild("HUDLabel");
                text.GetComponent<TextMesh>().text = statDisplayStrings[rand2];
                text.GetChild(0).GetComponent<TextMesh>().text = statDisplayStrings[rand2];
                statDisplayStrings.RemoveAt(rand2);

                statIndex++;
            }
        }
    }
}

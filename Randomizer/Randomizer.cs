using MSCLoader;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine.UI;

namespace Randomizer
{
    public class Randomizer : Mod
    {
        public override string ID => "Bp_Randomizer"; // Your (unique) mod ID 
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

        SettingsCheckBox reRollOverTime;
        internal SettingsTextBox minWait;
        internal SettingsTextBox maxWait;

        SettingsCheckBox ignoreWindshield;

        SettingsCheckBox randomGUI;
        SettingsCheckBox randomSubtitles;

        SettingsCheckBox randomRollingSounds;

        SettingsCheckBox randomGravity;
        SettingsTextBox randomGravitySideways;
        SettingsTextBox randomGravityMinY;
        SettingsTextBox randomGravityMaxY;

        private void Mod_Settings()
        {
            Settings.AddText(this, "<size=30><b><color=red>Disabling options will not reset anything, you need to restart for that!</color></b></size>");

            scrambleBind = Keybind.Add(this, "scrambleBind", "Randomize", KeyCode.R, KeyCode.LeftControl);

            reRollOverTime = Settings.AddCheckBox(this, "reroll", "Re-roll over time", false, SetReRoll);
            minWait = Settings.AddTextBox(this, "minWait", "Min wait", "15", "", InputField.ContentType.DecimalNumber);
            maxWait = Settings.AddTextBox(this, "maxWait", "Max wait", "120", "", InputField.ContentType.DecimalNumber);

            ignoreWindshield = Settings.AddCheckBox(this, "windshield", "Ignore windshields (so they stay see through)", true);

            randomGUI = Settings.AddCheckBox(this, "gui", "Random GUI", true);

            randomSubtitles = Settings.AddCheckBox(this, "subtitles", "Random Subtitles", true);

            Settings.AddText(this, "<b><color=red>The following setting might make loud sounds play when you drive around, not recommended but i made it:))</color></b>");
            randomRollingSounds = Settings.AddCheckBox(this, "rollingSounds", "Random wheel rolling sounds", false);

            randomGravity = Settings.AddCheckBox(this, "gravity", "Random gravity", false);
            randomGravitySideways = Settings.AddTextBox(this, "sideways", "Sideways", "0", "", InputField.ContentType.DecimalNumber);
            randomGravityMinY = Settings.AddTextBox(this, "min", "Min down", "3", "", InputField.ContentType.DecimalNumber);
            randomGravityMaxY = Settings.AddTextBox(this, "max", "Max down", "10", "", InputField.ContentType.DecimalNumber);
        }

        void SetReRoll()
        {
            if (reRoller != null)
                reRoller.enabled = reRollOverTime.GetValue();
        }

        //Camera mainCam;
        //Matrix4x4 defMatrix;

        ReRoller reRoller;
        //Unloader unloader;
        private void Mod_PostLoad()
        {
            //mainCam = Camera.main;
            //defMatrix = mainCam.projectionMatrix;

            GameObject player = GameObject.Find("PLAYER");
            reRoller = player.AddComponent<ReRoller>();
            reRoller.randomizer = this;
            reRoller.enabled = reRollOverTime.GetValue();
            //unloader = player.AddComponent<Unloader>();

            GetSubtitleVariables();
            GetStats();
            Randomize();
        }

        void Mod_Update()
        {
            if (scrambleBind.GetKeybindDown())
                Randomize();
        }

        FsmString[] subtitleVars;
        string[] subtitleStrings;
        void GetSubtitleVariables()
        {
            FsmString guiSubtitle = FsmVariables.GlobalVariables.GetFsmString("GUIsubtitle");

            List<FsmString> subtitles = new List<FsmString>();
            foreach (PlayMakerFSM fsm in Resources.FindObjectsOfTypeAll<PlayMakerFSM>())
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

        bool firstPass = true;
        internal void Randomize()
        {
            List<Material> mats = new List<Material>();
            List<Material> windshieldMats = new List<Material>();
            List<Renderer> renderers = new List<Renderer>();
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (!(renderer.material.shader.name == "GUI/Text Shader"))
                {
                    if (ignoreWindshield.GetValue() && renderer.material.shader.name == "Windshield/windshield")
                        windshieldMats.Add(renderer.material);
                    else
                    {
                        mats.Add(renderer.material);
                        renderers.Add(renderer);
                    }
                }
            }
            foreach (Renderer renderer in renderers)
            {
                int rand = Random.Range(0, mats.Count);
                renderer.material = mats[rand];
                mats.RemoveAt(rand);
            }
            firstPass = false;

            foreach (Material mat in windshieldMats)
            {
                Color color = GetRandomColor();
                color.a = mat.color.a;
                mat.color = color;
            }

            SetSkybox();
            SetLights();
            SetSounds();

            if (randomSubtitles.GetValue())
                SetSubtitles();

            if (randomGUI.GetValue())
                SetGUI();

            if (randomGravity.GetValue())
                SetGravity();

            //SetProjectionMatrix();
            //ModConsole.Log(mainCam.projectionMatrix);

            //unloader.StartUnload();
        }

        //void SetProjectionMatrix()
        //{
        //    Matrix4x4 matrix = defMatrix;
        //    for (int i = 0; i < 4; i++)
        //    {
        //        Vector4 vec = matrix.GetRow(i);
        //        vec.x += Random.Range(0f, 1f);
        //        vec.y += Random.Range(0f, 1f);
        //        vec.z += Random.Range(0f, 1f);
        //        vec.w += Random.Range(0f, 1f);
        //    }
        //    mainCam.projectionMatrix = matrix;
        //}

        void SetSubtitles()
        {
            List<string> subtitles = new List<string>(subtitleStrings);
            foreach (FsmString strVar in subtitleVars)
            {
                int rand = Random.Range(0, subtitles.Count);
                strVar.Value = subtitles[rand];
                subtitles.RemoveAt(rand);
            }
        }

        void SetSkybox()
        {
            RenderSettings.skybox.SetColor("_Tint", GetRandomColor());
            RenderSettings.skybox.SetColor("_SkyTint", GetRandomColor());
        }
        void SetLights()
        {
            foreach (Light light in Resources.FindObjectsOfTypeAll<Light>())
            {
                light.color = GetRandomColor();
            }
        }

        void SetSounds()
        {
            List<AudioClip> clips = new List<AudioClip>();
            List<AudioSource> audioSources = new List<AudioSource>();

            foreach (AudioSource ad in Resources.FindObjectsOfTypeAll<AudioSource>())
            {
                if (ad.clip != null)
                {
                    audioSources.Add(ad);
                    clips.Add(ad.clip);
                }
            }

            if (randomRollingSounds.GetValue())
            {
                SoundController[] soundControllers = Resources.FindObjectsOfTypeAll<SoundController>();
                foreach (SoundController sc in soundControllers)
                {
                    clips.Add(sc.rollingNoiseGrass);
                    clips.Add(sc.rollingNoiseOffroad);
                }

                foreach (SoundController sc in soundControllers)
                {
                    sc.rollingNoiseGrass = PickRandomAndDelete(clips);
                    sc.rollingNoiseOffroad = PickRandomAndDelete(clips);
                }
            }

            foreach (AudioSource ad in audioSources)
            {
                bool isPlaying = ad.isPlaying;
                ad.clip = PickRandomAndDelete(clips);
                if (isPlaying)
                    ad.Play();
            }


        }

        void SetGUI()
        {
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

        void SetGravity()
        {
            float x = float.Parse(randomGravitySideways.GetValue());
            float minY = float.Parse(randomGravityMinY.GetValue());
            float maxY = float.Parse(randomGravityMaxY.GetValue());
            Physics.gravity = new Vector3(Random.Range(-x, x), -Mathf.Abs(Random.Range(minY, maxY)), Random.Range(-x, x)); //abs cuz positive y gravity causes instant crash for some reason
        }

        AudioClip lastClip = null;
        AudioClip PickRandomAndDelete(List<AudioClip> clips)
        {
            int rand = Random.Range(0, clips.Count);
            AudioClip clip = clips[rand];
            clips.RemoveAt(rand);

            if (clip == null)
                return lastClip;
            
            lastClip = clip;
            return clip;
        }
    }

    internal class ReRoller : MonoBehaviour
    {
        internal Randomizer randomizer;
        
        void OnEnable()
        {
            StartCoroutine(Roller());
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        IEnumerator Roller()
        {
            while (true)
            {
                float wait = Random.Range(float.Parse(randomizer.minWait.GetValue()), float.Parse(randomizer.maxWait.GetValue()));
                yield return new WaitForSeconds(wait);
                randomizer.Randomize();
            }
        }
    }

    //internal class Unloader : MonoBehaviour
    //{
    //    internal void StartUnload()
    //    {
    //        StartCoroutine(Unload());
    //    }

    //    IEnumerator Unload()
    //    {
    //        AsyncOperation async = Resources.UnloadUnusedAssets();
    //        while (!async.isDone)
    //            yield return null;
    //    }
    //}
}

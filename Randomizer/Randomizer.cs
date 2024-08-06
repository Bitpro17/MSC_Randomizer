using MSCLoader;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine.UI;
using System.IO;
using System.Linq;

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

        SettingsCheckBox useCustomSounds;
        SettingsSlider customSoundRatio;

        SettingsCheckBox ignoreWindshield;

        SettingsCheckBox randomGUI;
        SettingsCheckBox randomSubtitles;

        SettingsCheckBox randomRollingSounds;

        SettingsCheckBox randomGravity;
        SettingsTextBox randomGravitySideways;
        SettingsTextBox randomGravityMinY;
        SettingsTextBox randomGravityMaxY;

        

        SettingsCheckBox randomMeshes;
        SettingsTextBox randomMeshMaxSizes;

        private void Mod_Settings()
        {
            Settings.AddText(this, "<size=30><b><color=red>Disabling options will not reset anything, you need to restart for that!</color></b></size>");

            scrambleBind = Keybind.Add(this, "scrambleBind", "Randomize", KeyCode.R, KeyCode.LeftControl);

            reRollOverTime = Settings.AddCheckBox(this, "reroll", "Re-roll over time", false, SetReRoll);
            minWait = Settings.AddTextBox(this, "minWait", "Min wait", "15", "", InputField.ContentType.DecimalNumber);
            maxWait = Settings.AddTextBox(this, "maxWait", "Max wait", "120", "", InputField.ContentType.DecimalNumber);

            useCustomSounds = Settings.AddCheckBox(this, "useCustom", "Use custom sounds (from asset folder)", true);
            customSoundRatio = Settings.AddSlider(this, "customRatio", "Custom sound amount (%)", 0f, 1f, 0.05f);

            randomMeshes = Settings.AddCheckBox(this, "meshes", "Random meshes", true);
            randomMeshMaxSizes = Settings.AddTextBox(this, "meshSizes", "Max randomized mesh radius (cm)    (so everything doesnt get screwed)", "20", "", InputField.ContentType.DecimalNumber);

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

            //unloader = player.AddComponent<Unloader>();

            CreateReRoller();

            GetMatsAndRenderers();
            GetSmallMeshesAndFilters();
            GetSoundsAndSources();

            GetAmbientColors();

            LoadAssetsFromFile();

            GetSubtitleVariables();
            GetStats();
            Randomize();
        }

        void CreateReRoller()
        {
            GameObject player = GameObject.Find("PLAYER");
            reRoller = player.AddComponent<ReRoller>();
            reRoller.randomizer = this;
            if (!reRollOverTime.GetValue())
                reRoller.enabled = false;
        }

        void Mod_Update()
        {
            if (scrambleBind.GetKeybindDown())
                Randomize();
        }


        void LoadAssetsFromFile()
        {
            AssetLoader al = GameObject.Find("PLAYER").AddComponent<AssetLoader>();
            al.randomizer = this;
            al.StartLoad();
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

        List<Material> materials = new List<Material>();
        List<Material> windshieldMaterials = new List<Material>();
        List<Renderer> renderers = new List<Renderer>();
        void GetMatsAndRenderers()
        {
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (!(renderer.material.shader.name == "GUI/Text Shader"))
                {
                    if (ignoreWindshield.GetValue() && renderer.material.shader.name == "Windshield/windshield")
                        windshieldMaterials.Add(renderer.material);
                    else
                    {
                        materials.Add(renderer.material);
                        renderers.Add(renderer);
                    }
                }
            }
        }

        FsmColor[] ambientColors;
        void GetAmbientColors()
        {
            PlayMakerFSM fsm = GameObject.Find("SUN/Pivot/SUN").GetComponent<PlayMakerFSM>();
            ambientColors = fsm.FsmVariables.ColorVariables;
            ModConsole.Log(fsm.name);
        }

        List<MeshFilter> smallMeshFilters = new List<MeshFilter>();
        List<Mesh> smallMeshes = new List<Mesh>();
        void GetSmallMeshesAndFilters()
        {
            foreach(MeshFilter filter in Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (filter.mesh.bounds.extents.magnitude < float.Parse(randomMeshMaxSizes.GetValue()) * 0.01f)
                {
                    smallMeshFilters.Add(filter);
                    smallMeshes.Add(filter.mesh);
                }
            }
        }

        List<AudioClip> clips = new List<AudioClip>();
        internal List<AudioClip> customClips = new List<AudioClip>();
        List<AudioSource> audioSources = new List<AudioSource>();
        SoundController[] soundControllers;
        void GetSoundsAndSources()
        {
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
                soundControllers = Resources.FindObjectsOfTypeAll<SoundController>();
                foreach (SoundController sc in soundControllers)
                {
                    clips.Add(sc.rollingNoiseGrass);
                    clips.Add(sc.rollingNoiseOffroad);
                }
            }
        }

        internal void Randomize()
        {
            List<Material> mats = new List<Material>(materials);
            foreach (Renderer renderer in renderers)
            {
                int rand = Random.Range(0, mats.Count);
                renderer.material = materials[rand];
                mats.RemoveAt(rand);
            }

            foreach (Material mat in windshieldMaterials)
            {
                Color color = GetRandomColor();
                color.a = mat.color.a;
                mat.color = color;
            }

            SetWorldColors();
            SetLights();
            SetSounds();

            if (randomSubtitles.GetValue())
                SetSubtitles();

            if (randomGUI.GetValue())
                SetGUI();

            if (randomGravity.GetValue())
                SetGravity();

            SetSmallMeshes();
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

        void SetSmallMeshes()
        {
            foreach (MeshFilter filter in smallMeshFilters)
            {
                filter.mesh = smallMeshes[Random.Range(0, smallMeshes.Count)];
            }
        }

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

        void SetWorldColors()
        {
            RenderSettings.skybox.SetColor("_Tint", GetRandomColor());
            RenderSettings.skybox.SetColor("_SkyTint", GetRandomColor());
            RenderSettings.fogColor = GetRandomColor();
            RenderSettings.ambientLight = GetRandomColor();
            RenderSettings.ambientGroundColor = GetRandomColor();
            RenderSettings.ambientEquatorColor = GetRandomColor();

            foreach(FsmColor color in ambientColors)
                color.Value = GetRandomColor();
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
            foreach (AudioSource ad in audioSources)
            {
                bool isPlaying = ad.isPlaying;
                ad.clip = PickRandomSound();
                if (isPlaying || ad.playOnAwake)
                    ad.Play();
            }

            if (randomRollingSounds.GetValue())
            {
                foreach (SoundController sc in soundControllers)
                {
                    sc.rollingNoiseGrass = PickRandomSound();
                    sc.rollingNoiseOffroad = PickRandomSound();
                }
            }

            AudioClip PickRandomSound()
            {
                return (Random.Range(0f, 1f) < customSoundRatio.GetValue() && customClips.Count > 1) ? customClips[Random.Range(0, customClips.Count)] : clips[Random.Range(0, clips.Count)];
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

        //AudioClip lastClip = null;
        //AudioClip PickRandomAndDelete(List<AudioClip> clips)
        //{
        //    int rand = Random.Range(0, clips.Count);
        //    AudioClip clip = clips[rand];
        //    if (clips.Count > 1)
        //    clips.RemoveAt(rand);

        //    if (clip == null)
        //        return lastClip;

        //    lastClip = clip;
        //    return clip;
        //}
    }

    internal class ReRoller : MonoBehaviour
    {
        internal Randomizer randomizer;
        
        void Start()
        {
            StopAllCoroutines();
            StartCoroutine(Roller());
        }

        void OnEnable()
        {
            StopAllCoroutines();
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

    internal class AssetLoader : MonoBehaviour
    {
        internal Randomizer randomizer;
        internal void StartLoad()
        {
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            int i = 0;
            foreach (string file in Directory.GetFiles(ModLoader.GetModAssetsFolder(randomizer)))
            {
                if (file.EndsWith(".wav"))
                {
                    WWW www = new WWW("file:///" + file);
                    yield return www;
                    randomizer.customClips.Add(www.GetAudioClip(false));
                    i++;
                }
            }
            ModConsole.Log($"[Randomizer] Loaded {i} sound assets");
        }
    }
}

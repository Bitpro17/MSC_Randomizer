using MSCLoader;
using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine.UI;
using System.Linq;


namespace Randomizer
{
    public class Randomizer : Mod
    {
        public override string ID => "Bp_Randomizer"; // Your (unique) mod ID 
        public override string Name => "Randomizer"; // Your mod name
        public override string Author => "Bitpro17"; // Name of the Author (your name)
        public override string Version => "0.3"; // Version
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

        internal SettingsCheckBox ignoreWindshield;

        SettingsCheckBox randomGUI;
        SettingsCheckBox randomSubtitles;

        internal SettingsCheckBox randomRollingSounds;

        SettingsCheckBox randomGravity;
        SettingsTextBox randomGravitySideways;
        SettingsTextBox randomGravityMinY;
        SettingsTextBox randomGravityMaxY;

        SettingsCheckBox randomAnimations;
        SettingsCheckBox randomTexts;
        

        internal SettingsCheckBox randomMeshes;
        internal SettingsTextBox randomMeshMaxSizes;
        SettingsSlider randomMeshChance;

        private void Mod_Settings()
        {
            Settings.AddText(this, "<size=30><b><color=red>Options marked with asterisk (*) only work in the main menu!</color></b></size>");

            scrambleBind = Keybind.Add(this, "scrambleBind", "Randomize", KeyCode.R, KeyCode.LeftControl);

            Settings.AddHeader(this, "Re rolling");
            reRollOverTime = Settings.AddCheckBox(this, "reroll", "Re-roll over time", true, SetReRoll);
            minWait = Settings.AddTextBox(this, "minWait", "Min wait", "15", "", InputField.ContentType.DecimalNumber);
            maxWait = Settings.AddTextBox(this, "maxWait", "Max wait", "120", "", InputField.ContentType.DecimalNumber);

            Settings.AddHeader(this, "Meshes");

            randomMeshes = Settings.AddCheckBox(this, "meshes", "* Random meshes", true);
            randomMeshMaxSizes = Settings.AddTextBox(this, "meshSizes", "* Max randomized mesh radius (cm)    (so everything doesnt get screwed)", "40", "", InputField.ContentType.DecimalNumber);
            randomMeshChance = Settings.AddSlider(this, "meshChance", "Random mesh chance", 0f, 1f, 0.5f);
            
            Settings.AddHeader(this, "Misc.");

            randomAnimations = Settings.AddCheckBox(this, "animations", "* Random animations", true);
            randomTexts = Settings.AddCheckBox(this, "texts", "* Randomize all text", true);

            useCustomSounds = Settings.AddCheckBox(this, "useCustom", "Use custom sounds (from asset folder)", true);
            customSoundRatio = Settings.AddSlider(this, "customRatio", "Custom sound amount (%)", 0f, 1f, 0.05f);

            ignoreWindshield = Settings.AddCheckBox(this, "windshield", "* Ignore windshields (so they stay see through)", true);

            randomGUI = Settings.AddCheckBox(this, "gui", "* Random GUI", true);

            randomSubtitles = Settings.AddCheckBox(this, "subtitles", "* Random Subtitles", true);

            //Settings.AddHeader(this, "Not recommended garbage", false);
            Settings.AddText(this, "<b><color=red>The following setting might make loud sounds play when you drive around, not recommended but i made it:))</color></b>");
            randomRollingSounds = Settings.AddCheckBox(this, "rollingSounds", "* Random wheel rolling sounds", false);

            randomGravity = Settings.AddCheckBox(this, "gravity", "* Random gravity", false);
            randomGravitySideways = Settings.AddTextBox(this, "sideways", "Sideways", "3", "", InputField.ContentType.DecimalNumber);
            randomGravityMinY = Settings.AddTextBox(this, "min", "Min down", "4", "", InputField.ContentType.DecimalNumber);
            randomGravityMaxY = Settings.AddTextBox(this, "max", "Max down", "12", "", InputField.ContentType.DecimalNumber);
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

            ReferenceGetter.r = this;
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>().Where(x => x.parent == null))
            { // add a script that gets references on awake, so when prefabs are instantiated their references are unique
                ReferenceGetter rg = t.gameObject.AddComponent<ReferenceGetter>();
                rg.isPrefab = false; //true by default, stays true for prefabs since you cant edit prefab variables:p
            }

            GetAmbientColors();

            GetSubtitleVariables();
            GetStats();

            if (useCustomSounds.GetValue())
                LoadAssetsFromFile();
            else
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

        internal List<Material> materials = new List<Material>();
        internal List<Material> windshieldMaterials = new List<Material>();
        internal List<Renderer> renderers = new List<Renderer>();

        FsmColor[] ambientColors;
        void GetAmbientColors()
        {
            PlayMakerFSM fsm = GameObject.Find("SUN/Pivot/SUN").GetComponent<PlayMakerFSM>();
            ambientColors = fsm.FsmVariables.ColorVariables;
        }

        internal List<MeshContainer> meshContainers = new List<MeshContainer>();
        internal class MeshContainer
        {
            internal MeshFilter meshFilter;
            internal Mesh originalMesh;

            internal MeshContainer(MeshFilter meshFilter)
            {
                this.meshFilter = meshFilter;
                this.originalMesh = meshFilter.mesh;
            }
        }

        internal List<AnimationContainer> animationContainers = new List<AnimationContainer>();
        internal class AnimationContainer
        {
            internal Animation animation;
            internal string[] names;
            internal AnimationContainer(Animation animation)
            {
                this.animation = animation;
                List<string> n = new List<string>();
                foreach(AnimationState state in animation)
                {
                    n.Add(state.name);
                }
                names = n.ToArray();
            }
        }

        internal List<AudioClip> clips = new List<AudioClip>();
        internal List<AudioClip> customClips = new List<AudioClip>();
        internal List<AudioSource> audioSources = new List<AudioSource>();

        internal List<TextMesh> textMeshes = new List<TextMesh>();
        internal List<string> texts = new List<string>();

        internal List<SoundController> soundControllers = new List<SoundController>();

        internal Material GetRandomMaterial()
        {
            return materials[Random.Range(0, materials.Count)];
        }

        internal void Randomize()
        {
            try
            {
                for (int i = 0; i < renderers.Count; i++)
                {
                    if (renderers[i] == null)
                    {
                        renderers.RemoveAt(i);
                        i--;
                    }
                    else
                        renderers[i].material = GetRandomMaterial();
                }
            }
            catch (System.Exception e)
            {
                LogError(e);
            }
            try
            {
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
            }
            catch (System.Exception e)
            {
                LogError(e);
            }
            try
            {
                if (randomMeshes.GetValue())
                    SetMeshes();
            }
            catch (System.Exception e)
            {
                LogError(e);
            }
            try
            {
                if (randomAnimations.GetValue())
                    SetAnimations();
            }
            catch (System.Exception e)
            {
                LogError(e);
            }
            try
            {
                if (randomTexts.GetValue())
                    SetTextMeshes();
            }
            catch (System.Exception e)
            {
                LogError(e);
            }
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


        void SetTextMeshes()
        {
            for (int i = 0; i < textMeshes.Count; i++)
            {
                if (textMeshes[i] == null)
                {
                    textMeshes.RemoveAt(i);
                    i--;
                }
                else if (textMeshes[i].gameObject.activeInHierarchy)
                {
                    textMeshes[i].text = texts[Random.Range(0, texts.Count)];

                    if (textMeshes[i].transform.childCount > 0)
                    {
                        TextMesh child = textMeshes[i].transform.GetChild(0).GetComponent<TextMesh>();
                        if (child != null)
                        {
                            child.text = textMeshes[i].text;
                        }
                    }
                }
            }
        }

        void SetAnimations()
        {
            for (int i = 0; i < animationContainers.Count; i++)
            {
                if (animationContainers[i].animation == null)
                {
                    animationContainers.RemoveAt(i);
                    i--;
                }
                else
                {
                    List<string> n = new List<string>(animationContainers[i].names);
                    foreach (AnimationState state in animationContainers[i].animation)
                    {
                        int rand = Random.Range(0, n.Count);
                        state.name = n[rand];
                        n.RemoveAt(rand);
                    }
                }
            }
        }

        MeshContainer[] changedMeshes = new MeshContainer[0];
        void SetMeshes()
        {
            int amount = (int)(meshContainers.Count * randomMeshChance.GetValue());

            foreach (MeshContainer mc in changedMeshes)
            {
                if (mc.meshFilter != null)
                {
                    mc.meshFilter.mesh = mc.originalMesh;
                }
            }

            changedMeshes = new MeshContainer[amount];

            for (int i = 0; i < amount; i++)
            {
                MeshContainer mc = meshContainers[Random.Range(0, meshContainers.Count)];
                if (mc.meshFilter == null)
                {
                    meshContainers.RemoveAt(i);
                    i--;
                }
                else
                {
                    mc.meshFilter.mesh = meshContainers[Random.Range(0, meshContainers.Count)].originalMesh;
                    changedMeshes[i] = mc;
                }
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

            //foreach(FsmColor color in ambientColors)
            //    color.Value = GetRandomColor();
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
            for (int i = 0; i < audioSources.Count; i++)
            {
                AudioSource ad = audioSources[i];

                if (ad == null)
                {
                    audioSources.RemoveAt(i);
                    i--;
                }
                else
                {
                    bool isPlaying = ad.isPlaying;
                    ad.clip = PickRandomSound();
                    if (ad.isActiveAndEnabled && (isPlaying || ad.playOnAwake))
                        ad.Play();
                }
            }

            if (randomRollingSounds.GetValue())
            {
                for (int i = 0; i < soundControllers.Count; i++)
                {
                    SoundController sc = soundControllers[i];
                    if (sc == null)
                    {
                        soundControllers.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        sc.rollingNoiseGrass = PickRandomSound();
                        sc.rollingNoiseOffroad = PickRandomSound();
                    }
                }
            }

            AudioClip PickRandomSound()
            {
                return (Random.Range(0f, 1f) < customSoundRatio.GetValue() && customClips.Count > 1) && useCustomSounds.GetValue() ? customClips[Random.Range(0, customClips.Count)] : clips[Random.Range(0, clips.Count)];
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

        void LogError(System.Exception e)
        {
            ModConsole.LogError("[Randomizer] did done screwed: " + e);
        }
    }
}

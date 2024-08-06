using UnityEngine;
using System.IO;
using System.Collections;
using MSCLoader;

namespace Randomizer
{
    internal class ReferenceGetter : MonoBehaviour
    {
        internal static Randomizer r;
        internal bool isPrefab = true;
        void Awake()
        {
            StartCoroutine(Wait());
        }
        
        IEnumerator Wait()
        {
            yield return null;
            GetMatsAndRenderers();
            GetMeshesAndFilters();
            GetSoundsAndSources();
            GetAnimations();
            GetTextMeshes();
            GetRollingSounds();
            Destroy(this);
        }

        void OnDestroy()
        {
            //foreach (SoundController sc in GetComponentsInChildren<SoundController>(true))
            //{
            //    r.soundControllers.Remove(sc);
            //}

            //foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            //{
            //    if (!(renderer.material.shader.name == "GUI/Text Shader"))
            //    {
            //        if (r.ignoreWindshield.GetValue() && renderer.material.shader.name == "Windshield/windshield")
            //            r.windshieldMaterials.Remove(renderer.material);
            //        else
            //        {
            //            r.renderers.Remove(renderer);
            //        }
            //    }
            //}
        }

        void GetRollingSounds()
        {
            if (r.randomRollingSounds.GetValue())
            {
                foreach (SoundController sc in GetComponentsInChildren<SoundController>(true))
                {
                    r.soundControllers.Add(sc);
                    r.clips.Add(sc.rollingNoiseGrass);
                    r.clips.Add(sc.rollingNoiseOffroad);
                }
            }
        }


        void GetMatsAndRenderers()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer.material.shader.name == "GUI/Text Shader"))
                {
                    if (r.ignoreWindshield.GetValue() && renderer.material.shader.name == "Windshield/windshield")
                        r.windshieldMaterials.Add(renderer.material);
                    else
                    {
                        r.materials.Add(renderer.material);
                        r.renderers.Add(renderer);
                        if (isPrefab)
                            renderer.material = r.GetRandomMaterial();
                    }
                }
            }
        }
        void GetMeshesAndFilters()
        {
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.mesh.bounds.extents.magnitude < float.Parse(r.randomMeshMaxSizes.GetValue()) * 0.01f)
                {
                    r.meshContainers.Add(new Randomizer.MeshContainer(filter));
                }
            }
        }

        void GetSoundsAndSources()
        {
            foreach (AudioSource ad in GetComponentsInChildren<AudioSource>(true))
            {
                if (ad.clip != null)
                {
                    r.audioSources.Add(ad);
                    r.clips.Add(ad.clip);
                }
            }

        }

        void GetAnimations()
        {
            foreach (Animation anim in GetComponentsInChildren<Animation>(true))
            {
                r.animationContainers.Add(new Randomizer.AnimationContainer(anim));
            }
        }

        void GetTextMeshes()
        {
            foreach(TextMesh mesh in GetComponentsInChildren<TextMesh>(true))
            {
                if (mesh.transform.parent.GetComponent<TextMesh>() == null) //isnt a shadow of another text
                {
                    r.textMeshes.Add(mesh);
                    r.texts.Add(mesh.text);
                }
            }
        }
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
            randomizer.Randomize();
        }
    }
}

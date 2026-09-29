#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Builds Soul Hunter's own projectiles, weapon effects, gameplay VFX and pickup polish (owner request 2026-09-25:
    /// "VFX, gems, chest, gold and bullets should be proper"). Before this, almost every weapon fired or placed the
    /// same orange sphere (Bullet.prefab), the Axe and Cross threw a cylinder and a magenta sphere, gameplay pickups
    /// were covered by placeholder cubes, and no particle effect existed. Area weapons also used the flying Bullet, so
    /// Santa Water pools, lightning strikes and Peachone blasts flew away with a 0.1-unit hit area.
    ///
    /// Everything is generated here from Unity meshes, the project's own SH10 models, procedural textures and particle
    /// systems (no external art). Weapon prefab fields are rewired to the new prefabs.
    /// Menu: Soul Hunter/Content/Build Weapon VFX, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.WeaponVfxBuilder.BuildAll -quit
    /// </summary>
    public static class WeaponVfxBuilder
    {
        public const string ProjectileFolder = "Assets/Prefabs/Weapons/Projectiles";
        public const string VfxFolder = "Assets/Prefabs/VFX";
        public const string TextureFolder = "Assets/Art/Textures/VFX";
        public const string MaterialFolder = "Assets/Art/Materials/VFX";
        public const string LibraryPath = "Assets/Resources/VfxLibrary.asset";
        private const string ParticleShader = "SoulHunter/VFX/Particle";

        private static Material _soft, _star, _ring, _smoke, _beam;
        // Owner-approved VFX v2 sprites (build_vfx_textures.py, 2026-09-25).
        private static Material _circle, _impact, _wisp, _flame, _slash;
        public const string SpriteFolder = "Assets/Art/Textures/VFX";
        private static Mesh _sphere, _cube, _quad;

        [MenuItem("Soul Hunter/Content/Build Weapon VFX")]
        public static void BuildAll()
        {
            foreach (var folder in new[] { ProjectileFolder, VfxFolder, TextureFolder, MaterialFolder }) EnsureFolder(folder);
            _sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            _cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            _quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            BuildTexturesAndMaterials();

            var library = BuildVfxLibrary();
            var p = BuildProjectiles(library);
            WireWeapons(p, library);
            PolishPickups();
            AssetDatabase.SaveAssets();
            Debug.Log($"[WeaponVfxBuilder] Built {p.Count} weapon prefabs and {8} gameplay effects.");
        }

        // ------------------------------------------------------------------ textures / materials

        private static void BuildTexturesAndMaterials()
        {
            var soft = Texture("SH10_Vfx_Soft", (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Pow(Mathf.Clamp01(1f - d), 2f); });
            var star = Texture("SH10_Vfx_Star", (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float cross = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(x) * 9f) * Mathf.Clamp01(1f - Mathf.Abs(y)), Mathf.Clamp01(1f - Mathf.Abs(y) * 9f) * Mathf.Clamp01(1f - Mathf.Abs(x)));
                return Mathf.Clamp01(cross * 0.9f + Mathf.Pow(Mathf.Clamp01(1f - d * 2.2f), 2f));
            });
            var ring = Texture("SH10_Vfx_Ring", (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) * 9f) * (d < 1f ? 1f : 0f); });
            var smoke = Texture("SH10_Vfx_Smoke", (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float n = Mathf.PerlinNoise(x * 3f + 7f, y * 3f + 3f) * 0.5f + Mathf.PerlinNoise(x * 7f + 1f, y * 7f + 9f) * 0.3f;
                return Mathf.Clamp01((1f - d) * 1.6f - 0.3f + n * 0.6f) * Mathf.Clamp01(1f - d);
            });
            var beamTex = Texture("SH10_Vfx_Beam", (x, y) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(y)), 1.5f));
            _beam = ParticleMaterial("SH10_Vfx_Beam", beamTex, additive: true);
            _soft = ParticleMaterial("SH10_Vfx_Soft", soft, additive: true);
            _star = ParticleMaterial("SH10_Vfx_Star", star, additive: true);
            _ring = ParticleMaterial("SH10_Vfx_Ring", ring, additive: true);
            _smoke = ParticleMaterial("SH10_Vfx_Smoke", smoke, additive: false);

            // VFX v2: the approved Soul Hunter sprites replace the plain stars and rings everywhere.
            _star = ParticleMaterial("SH10_Vfx2_Sparkle", Sprite2("sparkle"), additive: true);
            _ring = ParticleMaterial("SH10_Vfx2_ShockRing", Sprite2("shock_ring"), additive: true);
            _circle = ParticleMaterial("SH10_Vfx2_MagicCircle", Sprite2("magic_circle"), additive: true);
            _impact = ParticleMaterial("SH10_Vfx2_ImpactStar", Sprite2("impact_star"), additive: true);
            _wisp = ParticleMaterial("SH10_Vfx2_SoulWisp", Sprite2("soul_wisp"), additive: true);
            _flame = ParticleMaterial("SH10_Vfx2_SoulFlame", Sprite2("soul_flame"), additive: true);
            _slash = ParticleMaterial("SH10_Vfx2_SlashArc", Sprite2("slash_arc"), additive: true);
        }

        private static Texture2D Texture(string name, System.Func<float, float, float> alpha)
        {
            string path = $"{TextureFolder}/{name}.png";
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = (i + 0.5f) / size * 2f - 1f, y = (j + 0.5f) / size * 2f - 1f;
                    tex.SetPixel(i, j, new Color(1f, 1f, 1f, alpha(x, y)));
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>A VFX v2 sprite from build_vfx_textures.py (generated into SpriteFolder).</summary>
        private static Texture2D Sprite2(string name)
        {
            string path = $"{SpriteFolder}/SH10_Vfx2_{name}.png";
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                throw new FileNotFoundException("Run build_vfx_textures.py into " + SpriteFolder, path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material ParticleMaterial(string name, Texture2D texture, bool additive)
        {
            var mat = LoadOrCreateMaterial(name, Shader.Find(ParticleShader));
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Bright unlit colour for projectile cores (they read as glowing without HDR bloom).</summary>
        private static Material CoreMaterial(string name, Color color)
        {
            var mat = LoadOrCreateMaterial(name, Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            return mat;
        }

        // ------------------------------------------------------------------ particle helpers

        private sealed class Ps
        {
            public string Name = "Particles";
            public Material Material;
            public bool Loop;
            public bool World;
            public float Duration = 1f;
            public Vector2 Lifetime = new Vector2(0.3f, 0.5f);
            public Vector2 Speed = new Vector2(1f, 2f);
            public Vector2 Size = new Vector2(0.2f, 0.3f);
            public Color ColorA = Color.white, ColorB = Color.white;
            public Color EndColor = new Color(1f, 1f, 1f, 0f);
            public int Burst;
            public float Rate;
            public ParticleSystemShapeType Shape = ParticleSystemShapeType.Sphere;
            public float Radius = 0.1f;
            public Vector3 BoxSize = Vector3.one;
            public float Gravity;
            public Vector3 Velocity;
            public AnimationCurve SizeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            public bool Stretch;
            public float StretchLength = 2.5f;
            public bool HorizontalBillboard;
            public int MaxParticles = 60;
            public bool RandomRotation = true;   // off for sprites with a direction (wisp tails, flames)
            public float Spin;                   // degrees per second (magic circles turn)
        }

        private static ParticleSystem AddParticles(Transform parent, Ps s, Vector3 localPosition = default)
        {
            var go = new GameObject(s.Name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = s.Duration;
            main.loop = s.Loop;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(s.Lifetime.x, s.Lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(s.Speed.x, s.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(s.Size.x, s.Size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(s.ColorA, s.ColorB);
            main.startRotation = s.RandomRotation ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : new ParticleSystem.MinMaxCurve(0f);
            if (s.Spin != 0f)
            {
                var spin = ps.rotationOverLifetime;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(s.Spin * Mathf.Deg2Rad);
            }
            main.gravityModifier = s.Gravity;
            main.simulationSpace = s.World ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = s.MaxParticles;

            var emission = ps.emission;
            emission.rateOverTime = s.Rate;
            emission.SetBursts(s.Burst > 0 ? new[] { new ParticleSystem.Burst(0f, (short)s.Burst) } : new ParticleSystem.Burst[0]);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = s.Shape;
            shape.radius = s.Radius;
            shape.scale = s.Shape == ParticleSystemShapeType.Box ? s.BoxSize : Vector3.one;
            if (s.Shape == ParticleSystemShapeType.Circle) shape.rotation = new Vector3(90f, 0f, 0f);

            if (s.Velocity != Vector3.zero)
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(s.Velocity.x);
                vel.y = new ParticleSystem.MinMaxCurve(s.Velocity.y);
                vel.z = new ParticleSystem.MinMaxCurve(s.Velocity.z);
            }

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(s.EndColor.r, s.EndColor.g, s.EndColor.b), 1f) },
                             new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.4f), new GradientAlphaKey(s.EndColor.a, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, s.SizeCurve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = s.Material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (s.Stretch)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = s.StretchLength;
                renderer.velocityScale = 0.05f;
            }
            else if (s.HorizontalBillboard) renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            return ps;
        }

        /// <summary>
        /// Menu: Soul Hunter/Content/Build Blood VFX. Builds only the neck blood spray and sets it in VfxLibrary, leaving
        /// every other effect and weapon prefab as it is.
        /// CLI: -executeMethod SoulHunter.Editor.Content.WeaponVfxBuilder.BuildBloodOnly -quit
        /// </summary>
        [MenuItem("Soul Hunter/Content/Build Blood VFX")]
        public static void BuildBloodOnly()
        {
            foreach (var folder in new[] { VfxFolder, TextureFolder, MaterialFolder }) EnsureFolder(folder);
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            if (library == null) throw new FileNotFoundException("Build the weapon VFX first", LibraryPath);
            BuildBlood(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[WeaponVfxBuilder] Built the neck blood spray.");
        }

        private static readonly Color BloodBright = new Color(0.88f, 0.05f, 0.08f), BloodRed = new Color(0.7f, 0.02f, 0.06f);

        /// <summary>Three spurts, like a heartbeat, each weaker than the last: (start seconds, particles per step), 10 steps 0.02 s apart.</summary>
        public static readonly (float Time, int Count)[] SprayPulses = { (0f, 14), (0.25f, 10), (0.5f, 6) };
        public const int SprayStepsPerPulse = 10;
        public const float SprayStepSeconds = 0.02f;

        /// <summary>
        /// Blood on an enemy's death. Owner (2026-09-29): "blood effect enemy ke neck se spray particle ki tarah", then
        /// "jameen per blood effect nahi chahiye ... neck ke pass ... 300 particle mein dikhao". VS has blood that its
        /// "Disable blood" option switches off; the wiki does not show its look, so this is Soul Hunter's own.
        /// VFX_BloodSpray: exactly 300 particles (VFXPoolManager.SprayParticles) spurt from the neck in three pulses
        /// (140, 100 and 60 particles over 0.7 s) along the effect's +Y, which VFXPoolManager.PlayBlood turns away from the
        /// hero and upwards. Stretched streaks and small round drops in one system, in world space, so the jet arcs and
        /// falls under gravity; nothing is left on the ground. Alpha blended. Sizes, colours and timings are provisional.
        /// </summary>
        private static void BuildBlood(VfxLibrary library)
        {
            var dropTex = Texture("SH10_Vfx_BloodDrop", (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01((1f - d) * 3f); });
            var drop = ParticleMaterial("SH10_Vfx_BloodDrop", dropTex, additive: false);

            var spray = new GameObject("VFX_BloodSpray");
            var jet = AddParticles(spray.transform, new Ps
            {
                Name = "Jet", Material = drop, World = true, Duration = 0.8f,
                Lifetime = new Vector2(0.4f, 0.7f), Speed = new Vector2(3f, 6.5f), Size = new Vector2(0.05f, 0.14f),
                ColorA = BloodBright, ColorB = BloodRed, EndColor = new Color(0.5f, 0f, 0.04f, 0.85f),
                Gravity = 1.4f, Shape = ParticleSystemShapeType.Cone, Radius = 0.05f,
                SizeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.7f), Stretch = true, StretchLength = 1.2f,
                MaxParticles = VFXPoolManager.SprayParticles,
            });
            var shape = jet.shape;
            shape.angle = 14f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // the cone points along the effect's +Y
            var emission = jet.emission;
            emission.rateOverTime = 0f;
            var bursts = new ParticleSystem.Burst[SprayPulses.Length];
            for (int i = 0; i < SprayPulses.Length; i++)
                bursts[i] = new ParticleSystem.Burst(SprayPulses[i].Time, (short)SprayPulses[i].Count, SprayStepsPerPulse, SprayStepSeconds);
            emission.SetBursts(bursts);
            jet.GetComponent<ParticleSystemRenderer>().velocityScale = 0.06f;
            library.BloodSpray = SaveVfx(spray, VfxFolder);
            EditorUtility.SetDirty(library);
        }

        private static AnimationCurve Grow(float from, float to) => AnimationCurve.EaseInOut(0f, from, 1f, to);

        private static GameObject SaveVfx(GameObject root, string folder)
        {
            // Pooled objects keep their particles while inactive; clear them when reused so no stale trail flashes.
            if (folder == ProjectileFolder && root.GetComponentInChildren<ParticleSystem>() != null && root.GetComponent<ParticleReset>() == null)
                root.AddComponent<ParticleReset>();
            string path = $"{folder}/{root.name}.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ------------------------------------------------------------------ gameplay VFX

        private static readonly Color Soul = new Color(0.55f, 0.85f, 1f);
        private static readonly Color Gold = new Color(1f, 0.8f, 0.3f);

        private static VfxLibrary BuildVfxLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<VfxLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            var hit = new GameObject("VFX_HitSpark");
            AddParticles(hit.transform, new Ps { Name = "Flash", Material = _impact, Burst = 1, Lifetime = new Vector2(0.1f, 0.1f), Speed = Vector2.zero, Size = new Vector2(1.1f, 1.4f), ColorA = new Color(1f, 1f, 1f, 0.7f), ColorB = new Color(1f, 0.95f, 0.8f, 0.7f), Duration = 0.2f, MaxParticles = 2 });
            AddParticles(hit.transform, new Ps { Name = "Sparks", Material = _soft, Burst = 6, Lifetime = new Vector2(0.12f, 0.25f), Speed = new Vector2(4f, 8f), Size = new Vector2(0.08f, 0.14f), ColorA = Color.white, ColorB = new Color(1f, 0.85f, 0.4f), Stretch = true, Duration = 0.3f, MaxParticles = 12 });
            library.HitSpark = SaveVfx(hit, VfxFolder);

            var death = new GameObject("VFX_EnemyDeath");
            AddParticles(death.transform, new Ps { Name = "Puff", Material = _soft, Burst = 10, Lifetime = new Vector2(0.35f, 0.6f), Speed = new Vector2(1f, 2.5f), Size = new Vector2(0.4f, 0.8f), ColorA = new Color(0.6f, 0.4f, 1f, 0.8f), ColorB = new Color(0.35f, 0.8f, 1f, 0.8f), EndColor = new Color(0.2f, 0.4f, 0.9f, 0f), SizeCurve = Grow(0.8f, 1.5f), Radius = 0.3f, MaxParticles = 16 });
            AddParticles(death.transform, new Ps { Name = "Wisps", Material = _wisp, Burst = 4, Lifetime = new Vector2(0.6f, 0.9f), Speed = new Vector2(0.2f, 0.6f), Size = new Vector2(0.5f, 0.8f), ColorA = Soul, ColorB = new Color(0.7f, 0.6f, 1f), Velocity = new Vector3(0f, 2.8f, 0f), Radius = 0.3f, MaxParticles = 8, RandomRotation = false });
            library.EnemyDeath = SaveVfx(death, VfxFolder);
            BuildBlood(library);

            library.GemPickup = SaveVfx(Sparkle("VFX_GemPickup", new Color(0.4f, 0.95f, 1f)), VfxFolder);
            library.GoldPickup = SaveVfx(Sparkle("VFX_GoldPickup", Gold), VfxFolder);

            var heal = new GameObject("VFX_Heal");
            AddParticles(heal.transform, new Ps { Name = "Rise", Material = _star, Burst = 14, Lifetime = new Vector2(0.6f, 0.9f), Speed = new Vector2(0f, 0.3f), Size = new Vector2(0.2f, 0.35f), ColorA = new Color(0.4f, 1f, 0.5f), ColorB = new Color(0.8f, 1f, 0.7f), Velocity = new Vector3(0f, 2.2f, 0f), Shape = ParticleSystemShapeType.Circle, Radius = 0.7f, MaxParticles = 20 });
            AddParticles(heal.transform, new Ps { Name = "Ring", Material = _ring, Burst = 1, Lifetime = new Vector2(0.5f, 0.5f), Speed = Vector2.zero, Size = new Vector2(2.5f, 2.5f), ColorA = new Color(0.4f, 1f, 0.5f, 0.8f), SizeCurve = Grow(0.2f, 1f), HorizontalBillboard = true, MaxParticles = 2 }, new Vector3(0f, 0.1f, 0f));
            library.Heal = SaveVfx(heal, VfxFolder);

            var chest = new GameObject("VFX_ChestOpen");
            AddParticles(chest.transform, new Ps { Name = "Flash", Material = _soft, Burst = 1, Lifetime = new Vector2(0.2f, 0.2f), Speed = Vector2.zero, Size = new Vector2(3.5f, 3.5f), ColorA = new Color(1f, 0.9f, 0.5f, 0.8f), MaxParticles = 2 }, new Vector3(0f, 0.8f, 0f));
            AddParticles(chest.transform, new Ps { Name = "Ring", Material = _ring, Burst = 1, Lifetime = new Vector2(0.6f, 0.6f), Speed = Vector2.zero, Size = new Vector2(7f, 7f), ColorA = Gold, SizeCurve = Grow(0.1f, 1f), HorizontalBillboard = true, MaxParticles = 2 }, new Vector3(0f, 0.1f, 0f));
            AddParticles(chest.transform, new Ps { Name = "Coins", Material = _star, Burst = 40, Lifetime = new Vector2(0.6f, 1.1f), Speed = new Vector2(3f, 7f), Size = new Vector2(0.2f, 0.4f), ColorA = Gold, ColorB = Color.white, Gravity = 0.8f, Shape = ParticleSystemShapeType.Hemisphere, Radius = 0.4f, MaxParticles = 50 }, new Vector3(0f, 0.6f, 0f));
            library.ChestOpen = SaveVfx(chest, VfxFolder);

            var level = new GameObject("VFX_LevelUp");
            AddParticles(level.transform, new Ps { Name = "Circle", Material = _circle, Burst = 1, Lifetime = new Vector2(0.9f, 0.9f), Speed = Vector2.zero, Size = new Vector2(5f, 5f), ColorA = new Color(0.6f, 0.45f, 1f, 1f), SizeCurve = Grow(0.3f, 1f), HorizontalBillboard = true, MaxParticles = 2, RandomRotation = false, Spin = 120f }, new Vector3(0f, 0.08f, 0f));
            AddParticles(level.transform, new Ps { Name = "Ring", Material = _ring, Burst = 1, Lifetime = new Vector2(0.55f, 0.55f), Speed = Vector2.zero, Size = new Vector2(6f, 6f), ColorA = new Color(0.5f, 0.9f, 1f, 0.9f), SizeCurve = Grow(0.1f, 1f), HorizontalBillboard = true, MaxParticles = 2 }, new Vector3(0f, 0.1f, 0f));
            AddParticles(level.transform, new Ps { Name = "Pillar", Material = _star, Burst = 30, Lifetime = new Vector2(0.6f, 1f), Speed = new Vector2(0f, 0.4f), Size = new Vector2(0.2f, 0.4f), ColorA = new Color(0.5f, 0.9f, 1f), ColorB = Gold, Velocity = new Vector3(0f, 4f, 0f), Shape = ParticleSystemShapeType.Circle, Radius = 0.8f, MaxParticles = 40 });
            library.LevelUp = SaveVfx(level, VfxFolder);

            library.Explosion = SaveVfx(Explosion("VFX_Explosion", new Color(1f, 0.6f, 0.2f), 1f), VfxFolder);

            // The Whip had no visual: a slash arc lying on the ground, played along the lash (VFXPoolManager.Play with rotation).
            var slash = new GameObject("VFX_Slash");
            var arc = Core(slash.transform, "Arc", _quad, _slash, Vector3.one * 3f, new Vector3(90f, 0f, 0f));
            var fade = slash.AddComponent<VfxQuadFade>();
            fade.Tint = new Color(0.8f, 0.92f, 1f, 1f);
            library.Slash = SaveVfx(slash, VfxFolder);
            EditorUtility.SetDirty(library);
            return library;
        }

        private static GameObject Sparkle(string name, Color color)
        {
            var go = new GameObject(name);
            AddParticles(go.transform, new Ps { Name = "Stars", Material = _star, Burst = 6, Lifetime = new Vector2(0.25f, 0.4f), Speed = new Vector2(1.5f, 3f), Size = new Vector2(0.14f, 0.24f), ColorA = color, ColorB = Color.white, MaxParticles = 10 });
            AddParticles(go.transform, new Ps { Name = "Glow", Material = _soft, Burst = 1, Lifetime = new Vector2(0.15f, 0.15f), Speed = Vector2.zero, Size = new Vector2(0.8f, 0.8f), ColorA = new Color(color.r, color.g, color.b, 0.6f), MaxParticles = 2 });
            return go;
        }

        private static GameObject Explosion(string name, Color fire, float scale)
        {
            var go = new GameObject(name);
            AddParticles(go.transform, new Ps { Name = "Shock", Material = _ring, Burst = 1, Lifetime = new Vector2(0.35f, 0.35f), Speed = Vector2.zero, Size = new Vector2(4.5f * scale, 4.5f * scale), ColorA = new Color(1f, 0.75f, 0.35f, 0.9f), SizeCurve = Grow(0.15f, 1f), HorizontalBillboard = true, MaxParticles = 2 }, new Vector3(0f, 0.1f, 0f));
            AddParticles(go.transform, new Ps { Name = "Flash", Material = _impact, Burst = 1, Lifetime = new Vector2(0.12f, 0.12f), Speed = Vector2.zero, Size = new Vector2(3f * scale, 3f * scale), ColorA = new Color(1f, 0.9f, 0.6f, 0.9f), MaxParticles = 2 }, new Vector3(0f, 0.5f, 0f));
            AddParticles(go.transform, new Ps { Name = "Fire", Material = _flame, RandomRotation = false, Burst = 18, Lifetime = new Vector2(0.3f, 0.5f), Speed = new Vector2(2f * scale, 5f * scale), Size = new Vector2(0.5f * scale, 1f * scale), ColorA = new Color(1f, 0.9f, 0.4f), ColorB = fire, EndColor = new Color(0.8f, 0.2f, 0.05f, 0f), SizeCurve = Grow(1f, 0.3f), Radius = 0.3f * scale, MaxParticles = 24 }, new Vector3(0f, 0.5f, 0f));
            AddParticles(go.transform, new Ps { Name = "Smoke", Material = _smoke, Burst = 8, Lifetime = new Vector2(0.6f, 0.9f), Speed = new Vector2(0.5f, 1.5f), Size = new Vector2(1f * scale, 1.6f * scale), ColorA = new Color(0.2f, 0.18f, 0.2f, 0.7f), ColorB = new Color(0.35f, 0.3f, 0.3f, 0.6f), EndColor = new Color(0.3f, 0.3f, 0.3f, 0f), SizeCurve = Grow(0.6f, 1.3f), Velocity = new Vector3(0f, 1f, 0f), Radius = 0.4f * scale, MaxParticles = 10 }, new Vector3(0f, 0.5f, 0f));
            AddParticles(go.transform, new Ps { Name = "Sparks", Material = _soft, Burst = 10, Lifetime = new Vector2(0.2f, 0.4f), Speed = new Vector2(6f * scale, 10f * scale), Size = new Vector2(0.08f, 0.14f), ColorA = Color.white, ColorB = new Color(1f, 0.8f, 0.3f), Stretch = true, MaxParticles = 12 }, new Vector3(0f, 0.5f, 0f));
            return go;
        }

        // ------------------------------------------------------------------ weapon prefabs

        private enum Body { Flying, Physics, Zone, VisualOnly }

        /// <summary>
        /// A weapon object with exactly the components its weapon code expects: Flying = Projectile-driven (moves
        /// forward, deals its own damage); Physics = Rigidbody velocity + ProjectileDamage (Bone, Runetracer, Axe);
        /// Zone = stays put, damage added by the weapon (Santa Water, lightning, Peachone); VisualOnly = no collider.
        /// </summary>
        private static GameObject NewWeaponObject(string name, Body body, float radius)
        {
            var root = new GameObject(name);
            if (body == Body.VisualOnly) return root;
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = body != Body.Physics;
            rb.interpolation = RigidbodyInterpolation.None;
            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = radius;
            if (body == Body.Flying)
            {
                var projectile = root.AddComponent<Projectile>();
                var so = new SerializedObject(projectile);
                so.FindProperty("_speed").floatValue = 10f;
                so.FindProperty("_lifetime").floatValue = 3f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return root;
        }

        private static GameObject Core(Transform parent, string name, Mesh mesh, Material mat, Vector3 scale, Vector3 euler = default, Vector3 position = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static GameObject Model(Transform parent, string prefabPath, float worldSize, Vector3 euler)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new FileNotFoundException("Missing model", prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 0.01f);
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = Vector3.one * (worldSize / longest);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Transform Spinner(Transform parent, Vector3 axis, float speed)
        {
            var pivot = new GameObject("Spin");
            pivot.transform.SetParent(parent, false);
            var spin = pivot.AddComponent<VfxSpin>();
            spin.Axis = axis;
            spin.DegreesPerSecond = speed;
            return pivot.transform;
        }

        private static void Trail(Transform parent, Material mat, Color color, float size, float lifetime, float rate = 50f)
        {
            AddParticles(parent, new Ps { Name = "Trail", Material = mat, Loop = true, World = true, Rate = rate, Burst = 0, Lifetime = new Vector2(lifetime, lifetime), Speed = Vector2.zero, Size = new Vector2(size * 0.8f, size), ColorA = color, ColorB = color, EndColor = new Color(color.r, color.g, color.b, 0f), Radius = 0.02f, MaxParticles = 40 });
        }

        private static void Halo(Transform parent, Color color, float size)
        {
            AddParticles(parent, new Ps { Name = "Halo", Material = _soft, Loop = true, Rate = 20f, Lifetime = new Vector2(0.1f, 0.12f), Speed = Vector2.zero, Size = new Vector2(size * 0.9f, size), ColorA = new Color(color.r, color.g, color.b, 0.55f), ColorB = new Color(color.r, color.g, color.b, 0.45f), EndColor = new Color(color.r, color.g, color.b, 0.3f), SizeCurve = AnimationCurve.Constant(0f, 1f, 1f), Radius = 0.001f, MaxParticles = 4 });
        }

        private static Dictionary<string, GameObject> BuildProjectiles(VfxLibrary library)
        {
            var result = new Dictionary<string, GameObject>();
            void Save(GameObject root) => result[root.name] = SaveVfx(root, ProjectileFolder);

            var magic = new Color(0.45f, 0.95f, 1f);
            var root = NewWeaponObject("SH10_Proj_MagicBolt", Body.Flying, 0.3f);
            Core(root.transform, "Core", _sphere, CoreMaterial("SH10_Core_Magic", new Color(0.8f, 1f, 1f)), Vector3.one * 0.28f);
            Halo(root.transform, magic, 0.9f);
            Trail(root.transform, _soft, magic, 0.3f, 0.25f);
            Save(root);

            root = NewWeaponObject("SH10_Proj_Fireball", Body.Flying, 0.35f);
            Core(root.transform, "Core", _sphere, CoreMaterial("SH10_Core_Fire", new Color(1f, 0.85f, 0.4f)), Vector3.one * 0.34f);
            Halo(root.transform, new Color(1f, 0.5f, 0.1f), 1.1f);
            AddParticles(root.transform, new Ps { Name = "Flames", Material = _flame, Loop = true, World = true, Rate = 45f, RandomRotation = false, Velocity = new Vector3(0f, 1.2f, 0f), Lifetime = new Vector2(0.2f, 0.35f), Speed = new Vector2(0.1f, 0.4f), Size = new Vector2(0.45f, 0.65f), ColorA = new Color(1f, 0.8f, 0.3f), ColorB = new Color(1f, 0.45f, 0.1f), EndColor = new Color(0.7f, 0.1f, 0.05f, 0f), Radius = 0.12f, MaxParticles = 40 });
            Save(root);

            var holy = new Color(1f, 0.92f, 0.6f);
            root = NewWeaponObject("SH10_Proj_HolyBolt", Body.Flying, 0.3f);
            Core(root.transform, "Core", _sphere, CoreMaterial("SH10_Core_Holy", new Color(1f, 0.97f, 0.85f)), new Vector3(0.18f, 0.18f, 0.6f));
            Halo(root.transform, holy, 0.8f);
            Trail(root.transform, _star, holy, 0.25f, 0.22f);
            Save(root);

            root = NewWeaponObject("SH10_Proj_GunBullet", Body.Flying, 0.2f);
            Core(root.transform, "Core", _sphere, CoreMaterial("SH10_Core_Bullet", new Color(1f, 0.9f, 0.45f)), new Vector3(0.1f, 0.1f, 0.4f));
            Trail(root.transform, _soft, new Color(1f, 0.8f, 0.4f), 0.12f, 0.08f, 60f);
            Save(root);

            root = NewWeaponObject("SH10_Proj_Knife", Body.Flying, 0.25f);
            Model(root.transform, "Assets/Prefabs/Weapons/SH10_Knife.prefab", 0.65f, new Vector3(90f, 0f, 0f));
            Trail(root.transform, _soft, new Color(0.85f, 0.9f, 1f, 0.5f), 0.1f, 0.12f, 30f);
            Save(root);

            root = NewWeaponObject("SH10_Proj_ThousandEdge", Body.Flying, 0.25f);
            Model(root.transform, "Assets/Prefabs/Weapons/SH10_ThousandEdge.prefab", 0.65f, new Vector3(90f, 0f, 0f));
            Trail(root.transform, _soft, new Color(0.7f, 0.85f, 1f, 0.6f), 0.12f, 0.15f, 30f);
            Save(root);

            var mana = new Color(0.7f, 0.55f, 1f);
            root = NewWeaponObject("SH10_Proj_ManaShot", Body.Flying, 0.35f);
            Core(root.transform, "Core", _sphere, CoreMaterial("SH10_Core_Mana", new Color(0.85f, 0.8f, 1f)), new Vector3(0.3f, 1.1f, 0.3f), default, new Vector3(0f, 0.5f, 0f));
            Halo(root.transform, mana, 1.2f);
            Trail(root.transform, _star, mana, 0.3f, 0.3f);
            Save(root);

            root = NewWeaponObject("SH10_Proj_CherryBomb", Body.Flying, 0.35f);
            var spin = Spinner(root.transform, Vector3.up, 240f);
            Model(spin, "Assets/Prefabs/Weapons/SH10_CherryBomb.prefab", 0.6f, Vector3.zero);
            AddParticles(root.transform, new Ps { Name = "Fuse", Material = _soft, Loop = true, Rate = 30f, Lifetime = new Vector2(0.1f, 0.2f), Speed = new Vector2(0.5f, 1.5f), Size = new Vector2(0.08f, 0.14f), ColorA = new Color(1f, 0.8f, 0.3f), ColorB = Color.white, Radius = 0.03f, MaxParticles = 10 }, new Vector3(0f, 0.4f, 0f));
            root.AddComponent<SpawnVfxOnDisable>().Kind = VfxKind.Explosion;
            Save(root);

            root = NewWeaponObject("SH10_Proj_Bone", Body.Physics, 0.3f);
            spin = Spinner(root.transform, Vector3.up, 720f);
            Model(spin, "Assets/Prefabs/Weapons/SH10_Bone.prefab", 0.7f, new Vector3(90f, 0f, 0f));
            Save(root);

            var rune = new Color(0.4f, 0.7f, 1f);
            root = NewWeaponObject("SH10_Proj_Rune", Body.Physics, 0.3f);
            spin = Spinner(root.transform, Vector3.up, 360f);
            Core(spin, "Core", _cube, CoreMaterial("SH10_Core_Rune", new Color(0.6f, 0.85f, 1f)), Vector3.one * 0.26f, new Vector3(45f, 0f, 45f));
            Halo(root.transform, rune, 0.9f);
            Trail(root.transform, _soft, rune, 0.25f, 0.3f, 40f);
            Save(root);

            root = new GameObject("SH10_Proj_Axe");
            var axeBody = root.AddComponent<Rigidbody>();
            axeBody.useGravity = false;
            var axeCollider = root.AddComponent<CapsuleCollider>();
            axeCollider.isTrigger = true;
            axeCollider.radius = 0.35f;
            axeCollider.height = 1.2f;
            Model(root.transform, "Assets/Prefabs/Weapons/SH10_Axe.prefab", 0.9f, Vector3.zero);
            Save(root);

            root = new GameObject("SH10_Proj_Cross");
            var crossCollider = root.AddComponent<SphereCollider>();
            crossCollider.isTrigger = true;
            crossCollider.radius = 0.4f;
            spin = Spinner(root.transform, Vector3.up, 720f);
            Model(spin, "Assets/Prefabs/Weapons/SH10_Cross.prefab", 0.8f, new Vector3(90f, 0f, 0f));
            Halo(root.transform, holy, 1f);
            Save(root);

            // Area effects: stay where they are placed and keep a hit area that matches what is drawn.
            var water = new Color(0.35f, 0.65f, 1f);
            root = NewWeaponObject("SH10_Fx_HolyWaterPool", Body.Zone, 1.4f);
            var pool = Core(root.transform, "Pool", _quad, _soft, Vector3.one * 3.4f, new Vector3(90f, 0f, 0f), new Vector3(0f, 0.05f, 0f));
            pool.GetComponent<MeshRenderer>().sharedMaterial = ParticleTint("SH10_Vfx_Pool", _soft, new Color(0.35f, 0.65f, 1f, 0.75f));
            var rim = Core(root.transform, "Rim", _quad, _ring, Vector3.one * 3.2f, new Vector3(90f, 0f, 0f), new Vector3(0f, 0.06f, 0f));
            rim.GetComponent<MeshRenderer>().sharedMaterial = ParticleTint("SH10_Vfx_PoolRim", _circle, new Color(0.5f, 0.8f, 1f, 0.9f));
            var rimSpin = rim.AddComponent<VfxSpin>();
            rimSpin.Axis = Vector3.forward; // the quad lies flat: its local Z is the world up axis
            rimSpin.DegreesPerSecond = 40f;
            AddParticles(root.transform, new Ps { Name = "Bubbles", Material = _soft, Loop = true, Rate = 16f, Lifetime = new Vector2(0.5f, 0.8f), Speed = Vector2.zero, Size = new Vector2(0.15f, 0.3f), ColorA = water, ColorB = Color.white, Velocity = new Vector3(0f, 1f, 0f), Shape = ParticleSystemShapeType.Circle, Radius = 1.3f, MaxParticles = 20 });
            AddParticles(root.transform, new Ps { Name = "Splash", Material = _star, Burst = 16, Lifetime = new Vector2(0.3f, 0.5f), Speed = new Vector2(2f, 4f), Size = new Vector2(0.15f, 0.25f), ColorA = water, ColorB = Color.white, Gravity = 1f, Shape = ParticleSystemShapeType.Hemisphere, Radius = 0.3f, MaxParticles = 20 });
            Save(root);

            root = NewWeaponObject("SH10_Fx_LightningStrike", Body.Zone, 0.9f);
            var boltGo = new GameObject("Bolt");
            boltGo.transform.SetParent(root.transform, false);
            var line = boltGo.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 9;
            line.widthCurve = AnimationCurve.Linear(0f, 0.18f, 1f, 0.4f);
            line.sharedMaterial = _beam;
            line.startColor = new Color(0.8f, 0.9f, 1f);
            line.endColor = Color.white;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.numCornerVertices = 2;
            boltGo.AddComponent<LightningBolt>();
            AddParticles(root.transform, new Ps { Name = "Flash", Material = _impact, Burst = 1, Lifetime = new Vector2(0.15f, 0.15f), Speed = Vector2.zero, Size = new Vector2(2.6f, 2.6f), ColorA = new Color(0.8f, 0.9f, 1f, 0.9f), MaxParticles = 2 }, new Vector3(0f, 0.3f, 0f));
            AddParticles(root.transform, new Ps { Name = "Sparks", Material = _soft, Burst = 10, Lifetime = new Vector2(0.15f, 0.3f), Speed = new Vector2(4f, 8f), Size = new Vector2(0.08f, 0.14f), ColorA = Color.white, ColorB = new Color(0.6f, 0.8f, 1f), Stretch = true, MaxParticles = 12 }, new Vector3(0f, 0.3f, 0f));
            Save(root);

            root = NewWeaponObject("SH10_Fx_PeachoneBlast", Body.Zone, 0.9f);
            var blast = Explosion("Blast", new Color(1f, 0.55f, 0.75f), 0.7f);
            blast.transform.SetParent(root.transform, false);
            Save(root);

            root = NewWeaponObject("SH10_Fx_PeachoneBird", Body.VisualOnly, 0f);
            Model(root.transform, "Assets/Prefabs/Weapons/SH10_Peachone.prefab", 1.1f, Vector3.zero);
            Trail(root.transform, _soft, new Color(1f, 0.75f, 0.85f, 0.6f), 0.2f, 0.25f, 25f);
            Save(root);

            root = NewWeaponObject("SH10_Fx_Pentagram", Body.VisualOnly, 0f);
            AddParticles(root.transform, new Ps { Name = "Flash", Material = _soft, Burst = 1, Lifetime = new Vector2(0.3f, 0.3f), Speed = Vector2.zero, Size = new Vector2(28f, 28f), ColorA = new Color(1f, 1f, 0.9f, 0.5f), MaxParticles = 2 }, new Vector3(0f, 1f, 0f));
            AddParticles(root.transform, new Ps { Name = "Circle", Material = _circle, Burst = 1, Lifetime = new Vector2(1.2f, 1.2f), Speed = Vector2.zero, Size = new Vector2(30f, 30f), ColorA = new Color(1f, 0.9f, 0.6f, 1f), SizeCurve = Grow(0.2f, 1f), HorizontalBillboard = true, MaxParticles = 2, RandomRotation = false, Spin = 60f }, new Vector3(0f, 0.2f, 0f));
            AddParticles(root.transform, new Ps { Name = "Ring", Material = _ring, Burst = 1, Lifetime = new Vector2(0.7f, 0.7f), Speed = Vector2.zero, Size = new Vector2(34f, 34f), ColorA = new Color(1f, 0.95f, 0.7f, 0.9f), SizeCurve = Grow(0.05f, 1f), HorizontalBillboard = true, MaxParticles = 2 }, new Vector3(0f, 0.2f, 0f));
            AddParticles(root.transform, new Ps { Name = "Stars", Material = _star, Burst = 60, Lifetime = new Vector2(0.5f, 1f), Speed = new Vector2(0f, 1f), Size = new Vector2(0.3f, 0.6f), ColorA = Color.white, ColorB = Gold, Velocity = new Vector3(0f, 2f, 0f), Radius = 12f, MaxParticles = 70 });
            Save(root);

            root = NewWeaponObject("SH10_Fx_FreezeBeam", Body.VisualOnly, 0f);
            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(root.transform, false);
            var beam = beamGo.AddComponent<LineRenderer>();
            beam.useWorldSpace = false;
            beam.positionCount = 2;
            beam.SetPosition(0, new Vector3(0f, 0.8f, 0.3f));
            beam.SetPosition(1, new Vector3(0f, 0.8f, 20f));
            beam.widthCurve = AnimationCurve.Linear(0f, 0.45f, 1f, 0.2f);
            beam.sharedMaterial = _beam;
            beam.startColor = new Color(0.7f, 0.95f, 1f);
            beam.endColor = new Color(0.4f, 0.8f, 1f, 0.3f);
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AddParticles(root.transform, new Ps { Name = "Ice", Material = _star, Burst = 30, Lifetime = new Vector2(0.3f, 0.5f), Speed = new Vector2(0f, 0.5f), Size = new Vector2(0.15f, 0.3f), ColorA = new Color(0.7f, 0.95f, 1f), ColorB = Color.white, Shape = ParticleSystemShapeType.Box, BoxSize = new Vector3(0.3f, 0.3f, 19f), MaxParticles = 40 }, new Vector3(0f, 0.8f, 10f));
            Save(root);

            return result;
        }

        private static Material ParticleTint(string name, Material source, Color tint)
        {
            var mat = LoadOrCreateMaterial(name, source.shader);
            mat.CopyPropertiesFromMaterial(source);
            mat.SetColor("_Color", tint);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ wiring

        /// <summary>(weapon prefab, component field) -> new prefab name.</summary>
        public static readonly (string Prefab, string Field, string Target)[] Wiring =
        {
            ("Assets/Prefabs/Weapons/Wand_Weapon.prefab", "_projectilePrefab", "SH10_Proj_MagicBolt"),
            ("Assets/Prefabs/Weapons/Generated/Knife.prefab", "_projectilePrefab", "SH10_Proj_Knife"),
            ("Assets/Prefabs/Weapons/Generated/ThousandEdge.prefab", "KnifePrefab", "SH10_Proj_ThousandEdge"),
            ("Assets/Prefabs/Weapons/Generated/FireWand.prefab", "FireballPrefab", "SH10_Proj_Fireball"),
            ("Assets/Prefabs/Weapons/Generated/HolyWand.prefab", "ProjectilePrefab", "SH10_Proj_HolyBolt"),
            ("Assets/Prefabs/Weapons/Generated/Gun.prefab", "BulletPrefab", "SH10_Proj_GunBullet"),
            ("Assets/Prefabs/Weapons/Generated/SongOfMana.prefab", "VerticalBeamPrefab", "SH10_Proj_ManaShot"),
            ("Assets/Prefabs/Weapons/Generated/CherryBomb.prefab", "BombPrefab", "SH10_Proj_CherryBomb"),
            ("Assets/Prefabs/Weapons/Generated/CherryBomb.prefab", "ExplosionPrefab", "SH10_Fx_PeachoneBlast"),
            ("Assets/Prefabs/Weapons/Generated/Bone.prefab", "BonePrefab", "SH10_Proj_Bone"),
            ("Assets/Prefabs/Weapons/Generated/Runetracer.prefab", "RunePrefab", "SH10_Proj_Rune"),
            ("Assets/Prefabs/Weapons/Axe_Weapon.prefab", "_axePrefab", "SH10_Proj_Axe"),
            ("Assets/Prefabs/Weapons/Cross_Weapon.prefab", "_crossPrefab", "SH10_Proj_Cross"),
            ("Assets/Prefabs/Weapons/Generated/HeavenSword.prefab", "_crossPrefab", "SH10_Proj_Cross"),
            ("Assets/Prefabs/Weapons/Generated/SantaWater.prefab", "WaterZonePrefab", "SH10_Fx_HolyWaterPool"),
            ("Assets/Prefabs/Weapons/Generated/LightningRing.prefab", "LightningStrikePrefab", "SH10_Fx_LightningStrike"),
            ("Assets/Prefabs/Weapons/Generated/Peachone.prefab", "BombPrefab", "SH10_Fx_PeachoneBlast"),
            ("Assets/Prefabs/Weapons/Generated/Peachone.prefab", "BirdVisual", "SH10_Fx_PeachoneBird"),
            ("Assets/Prefabs/Weapons/Generated/Pentagram.prefab", "PentagramVFXPrefab", "SH10_Fx_Pentagram"),
            ("Assets/Prefabs/Weapons/Generated/ClockLancet.prefab", "FreezeBeamPrefab", "SH10_Fx_FreezeBeam"),
        };

        private static void WireWeapons(Dictionary<string, GameObject> built, VfxLibrary library)
        {
            foreach (var (prefabPath, field, target) in Wiring)
            {
                var contents = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    bool set = false;
                    foreach (var weapon in contents.GetComponentsInChildren<AutoAttackWeapon>(true))
                    {
                        var so = new SerializedObject(weapon);
                        var property = so.FindProperty(field);
                        if (property == null) continue;
                        property.objectReferenceValue = built[target];
                        so.ApplyModifiedPropertiesWithoutUndo();
                        set = true;
                    }
                    if (!set) throw new System.InvalidOperationException($"{prefabPath} has no field {field}.");
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
        }

        // ------------------------------------------------------------------ pickups

        private static void PolishPickups()
        {
            // The gameplay pickups wrapped their SH10 model in a placeholder cube that hid it (the yellow squares).
            // Visual scales: from the tilted camera the models read too small next to the 2-unit heroes.
            // XP gems, the chest and the gold coin now come from PickupV2Importer (owner-approved designs, 2026-09-25).
            HidePlaceholder("Assets/Prefabs/Items/HealthChiken.prefab", bob: 0.06f, spin: 60f, glow: Color.black, scale: 1.2f);
            HidePlaceholder("Assets/Prefabs/Items/Magnet_Pickup.prefab", bob: 0.1f, spin: 90f, glow: new Color(0.2f, 0.05f, 0.05f));
            HidePlaceholder("Assets/Prefabs/Items/TimeFreeze_Pickup.prefab", bob: 0.1f, spin: 90f, glow: new Color(0.05f, 0.3f, 0.35f));
        }

        private static void HidePlaceholder(string path, float bob, float spin, Color glow, bool beacon = false, float scale = 1f)
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var rootRenderer = contents.GetComponent<MeshRenderer>();
                if (rootRenderer != null) rootRenderer.enabled = false;
                Transform visual = null;
                foreach (Transform child in contents.transform)
                    if (child.GetComponentInChildren<Renderer>() != null && child.name != "Beacon") visual = child;
                if (visual == null) throw new System.InvalidOperationException($"{path} has no model child.");
                var marker = visual.GetComponent<VfxScaled>();
                if (marker == null)
                {
                    visual.localScale *= scale; // once: re-running the builder must not grow it again
                    visual.gameObject.AddComponent<VfxScaled>();
                }
                var motion = contents.GetComponent<PickupIdleMotion>() ?? contents.AddComponent<PickupIdleMotion>();
                motion.Visual = visual;
                motion.BobHeight = bob;
                motion.SpinDegreesPerSecond = spin;
                if (glow != Color.black)
                    foreach (var r in visual.GetComponentsInChildren<Renderer>())
                        foreach (var mat in r.sharedMaterials)
                            if (mat != null && mat.HasProperty("_EmissionColor"))
                            {
                                mat.SetColor("_EmissionColor", glow);
                                EditorUtility.SetDirty(mat);
                            }
                var old = contents.transform.Find("Beacon");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                if (beacon)
                    AddParticles(contents.transform, new Ps { Name = "Beacon", Material = _star, Loop = true, Rate = 5f, Lifetime = new Vector2(0.8f, 1.2f), Speed = Vector2.zero, Size = new Vector2(0.15f, 0.3f), ColorA = Gold, ColorB = Color.white, Velocity = new Vector3(0f, 1.2f, 0f), Shape = ParticleSystemShapeType.Circle, Radius = 0.6f, MaxParticles = 10 });
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
#endif

using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Player;

namespace Rushitadel.Presentation
{
    // Presentation only. Never moves the hitbox, rig, projectile origin or aim direction.
    [DefaultExecutionOrder(40), DisallowMultipleComponent]
    public sealed class PlayerFlightEffects : MonoBehaviour
    {
        private sealed class Puff
        {
            public SpriteRenderer Sprite;
            public readonly MaterialPropertyBlock Properties = new();
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector2 Size;
            public float Age, Duration;
            public int Mode;
        }
        private readonly Puff[] pool = new Puff[40];
        private int cursor, trailCursor;
        private Material effectMaterial;
        private SpriteRenderer core, wake;
        private MaterialPropertyBlock wakeProperties;
        private PlayerAttackController attack;
        private PlayerPowerShotController power;
        private ModularPlayerArt art;
        private PlayerHealth health;
        private Vector3 lastPosition, drift, kick;
        private float clock, flash, nextTrail;
        private bool initialized;
        public Vector3 VisualOffsetWorld { get; private set; }
        public float HoverScale { get; private set; } = 1;
        public float CorePulse => 1 + .065f * Mathf.Sin(clock * 2.4f) + flash * .42f;
        public float ShotFlash => flash;
        public int ActivePuffs { get; private set; }
        public int ShotsShown { get; private set; }
        public int MovementTrailsShown { get; private set; }
        public int ActiveMovementPuffs { get; private set; }
        public Vector2 LastTrailDirection { get; private set; }

        public void Configure(ModularPlayerArt owner)
        {
            if (initialized) return;
            art = owner;
            var shader = Resources.Load<Shader>("PlayerForms/FlightVfx");
            if (shader == null) { Debug.LogError("Missing FlightVfx shader"); return; }
            effectMaterial = new Material(shader);
            wakeProperties = new MaterialPropertyBlock();
            core = Make("Reactor breathing", 108);
            wake = Make("Turbine slipstream", 99);
            wake.transform.localScale = Vector3.one * 1.52f;
            wakeProperties.SetFloat("_Mode", 4);
            wake.SetPropertyBlock(wakeProperties);
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = new Puff { Sprite = Make("Flight effect " + i, 109) };
                pool[i].Sprite.enabled = false;
            }
            attack = GetComponent<PlayerAttackController>();
            power = GetComponent<PlayerPowerShotController>();
            health = GetComponent<PlayerHealth>();
            if (attack != null) attack.Fired += OnAttack;
            if (power != null) power.Fired += OnPower;
            lastPosition = transform.position;
            initialized = true;
        }

        private SpriteRenderer Make(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = TransformationShot.Glyph;
            sprite.sharedMaterial = effectMaterial;
            sprite.sortingOrder = order;
            return sprite;
        }

        private void OnAttack(Vector2 direction)
        {
            flash = Mathf.Max(flash, .55f);
            kick = -(Vector3)direction * .023f;
            Emit(transform.position + (Vector3)direction * .18f, direction, new Vector2(.16f,.34f), .13f, 2);
            ShotsShown++;
        }

        private void OnPower(Vector2 direction)
        {
            flash = 1;
            Emit(transform.position, direction, Vector2.one * 1.18f, .28f, 1);
            Emit(transform.position + (Vector3)direction * .23f, direction, new Vector2(.34f,.6f), .2f, 2);
            ShotsShown++;
        }

        public void TurretShot(TowerController tower, int form)
        {
            if (!initialized || Time.timeScale <= 0 || health == null || !health.IsAlive) return;
            bool support = form == 8 || form == 14;
            bool spread = form == 2 || form == 6 || form == 7 || form == 11 || form == 15;
            bool heavy = form == 3 || form == 5;
            Vector2 size = support ? Vector2.one * .28f : spread ? new Vector2(.29f,.16f) : heavy ? new Vector2(.20f,.31f) : new Vector2(.11f,.19f);
            Emit(tower.MuzzlePosition, tower.transform.up, size, support ? .2f : heavy ? .16f : .085f, support ? 1 : 2);
            flash = Mathf.Max(flash, heavy ? .18f : .07f);
            ShotsShown++;
        }

        private void Emit(Vector3 position, Vector2 direction, Vector2 size, float duration, int mode)
        {
            if (!initialized) return;
            // Reserve eight slots for movement so rapid turret fire cannot erase the wake.
            var p = mode==3 ? pool[32+trailCursor++%8] : pool[cursor++%32];
            p.Position = position;
            p.Rotation = Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90);
            p.Size = size; p.Age = 0; p.Duration = duration; p.Mode = mode;
            p.Sprite.enabled = true;
            p.Sprite.sortingOrder=mode==3?96:109;
            Present(p);
        }

        private void Present(Puff p)
        {
            float t = Mathf.Clamp01(p.Age / p.Duration);
            p.Sprite.transform.SetPositionAndRotation(p.Position, p.Rotation);
            p.Sprite.transform.localScale = new Vector3(p.Size.x,p.Size.y,1) * (p.Mode == 1 ? 1 : Mathf.Lerp(1,.7f,t));
            Color c = art.EmissionColor;
            c.a = p.Mode == 3 ? .16f : .65f;
            p.Sprite.color = c;
            p.Properties.SetFloat("_Progress", t);
            p.Properties.SetFloat("_Mode", p.Mode);
            p.Sprite.SetPropertyBlock(p.Properties);
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (Time.timeScale <= 0 || health == null || !health.IsAlive)
            {
                lastPosition = transform.position;
                if(health==null||!health.IsAlive)ClearMovementTrail();
                return;
            }
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            clock += dt;
            Vector3 delta=transform.position-lastPosition;
            bool teleported=delta.sqrMagnitude>1;
            Vector3 velocity = teleported?Vector3.zero:Vector3.ClampMagnitude(delta/dt, 4);
            if(teleported)ClearMovementTrail();
            lastPosition = transform.position;
            float blend = 1-Mathf.Exp(-9*dt);
            drift = Vector3.Lerp(drift, -velocity*.007f, blend);
            kick = Vector3.Lerp(kick, Vector3.zero, 1-Mathf.Exp(-20*dt));
            flash = Mathf.MoveTowards(flash,0,dt*5);
            VisualOffsetWorld = drift+kick+Vector3.up*(.013f*Mathf.Sin(clock*2.4f));
            HoverScale = 1+.004f*Mathf.Sin(clock*2.4f);
            core.transform.position = transform.position+VisualOffsetWorld;
            core.transform.localScale = Vector3.one*(.29f+flash*.15f);
            var tint = art.EmissionColor; tint.a = .18f+flash*.42f; core.color=tint;
            wake.transform.localPosition = new Vector3(0,-.025f,0);
            wake.transform.localRotation = Quaternion.Euler(0,0,clock*-18);
            tint.a=.075f+Mathf.Clamp01(velocity.magnitude)*.025f; wake.color=tint;
            if (velocity.sqrMagnitude>.16f && clock>=nextTrail)
            {
                nextTrail=clock+.085f;
                Vector3 back=-velocity.normalized;
                Vector3 side=new Vector3(-back.y,back.x,0);
                float scale=Mathf.Abs(transform.lossyScale.x);
                float offset=(MovementTrailsShown%2==0?1:-1)*.23f*scale;
                Emit(transform.position+back*(.52f*scale)+side*offset,back,new Vector2(.12f,.42f),.32f,3);
                LastTrailDirection=back;
                MovementTrailsShown++;
            }
            ActivePuffs=0;ActiveMovementPuffs=0;
            foreach(var p in pool)
            {
                if(!p.Sprite.enabled) continue;
                p.Age+=dt;
                if(p.Age>=p.Duration) { p.Sprite.enabled=false; continue; }
                Present(p); ActivePuffs++;
                if(p.Mode==3)ActiveMovementPuffs++;
            }
        }

        private void ClearMovementTrail()
        {
            foreach(var p in pool)if(p!=null&&p.Mode==3)p.Sprite.enabled=false;
            ActiveMovementPuffs=0;
        }

        private void OnDisable()
        {
            foreach(var p in pool)if(p!=null)p.Sprite.enabled=false;
            ActiveMovementPuffs=ActivePuffs=0;lastPosition=transform.position;
        }

        private void OnDestroy()
        {
            if(attack!=null) attack.Fired-=OnAttack;
            if(power!=null) power.Fired-=OnPower;
            if(effectMaterial!=null) Destroy(effectMaterial);
        }
    }
}

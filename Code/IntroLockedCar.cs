using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vitmod
{
    [CustomEntity("vitellary/lockedintrocar")]
    [Tracked]
    public class IntroLockedCar : Solid
    {
        public IntroLockedCar(EntityData data, Vector2 offset, EntityID id) : base(data.Position + offset, 25f, 4f, true)
        {
            ID = id;
            if (!VitModule.Session.introCarEntityDatas.ContainsKey(ID))
            {
                VitModule.Session.introCarEntityDatas.Add(ID, data);
                VitModule.Session.introCarOffsets.Add(ID, offset);
            }
            opening = false;
            didHaveRider = false;
            startY = Y;
            Depth = 1;
            bodySprite = GFX.SpriteBank.Create("locked_intro_car_body");
            bodySprite.Y -= 9f;
            Add(bodySprite);
            Hitbox hitbox = new Hitbox(27f, 15f, -17f, -17f);
            Hitbox hitbox2 = new Hitbox(15f, 9f, 10f, -11f);
            Collider = new ColliderList(new Collider[]
            {
                hitbox,
                hitbox2
            });
            Add(new PlayerCollider(new Action<Player>(OnPlayer), new Circle(60f), null));
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            wheels = new Entity(Position);
            wheelSprite = GFX.SpriteBank.Create("locked_intro_car_wheels");
            wheelSprite.Y -= 9f;
            wheels.Add(wheelSprite);
            wheels.Depth = 3;
            Scene.Add(wheels);
        }

        public override void Update()
        {
            bool hasRider = HasRider();
            if (Y > startY && !hasRider)
            {
                MoveV(-1f);
            }
            if (Y <= startY && !didHaveRider && hasRider)
            {
                MoveV(1f);
            }
            if (didHaveRider && !hasRider)
            {
                Audio.Play("event:/game/00_prologue/car_up", Position);
            }
            didHaveRider = hasRider;
            base.Update();
        }

        public override int GetLandSoundIndex(Entity entity)
        {
            Audio.Play("event:/game/00_prologue/car_down", Position);
            return -1;
        }

        private void OnPlayer(Player player)
        {
            if (!opening)
            {
                foreach (Follower follower in player.Leader.Followers)
                {
                    if (follower.Entity is Key && !(follower.Entity as Key).StartedUsing)
                    {
                        TryOpen(player, follower);
                        break;
                    }
                }
            }
        }

        private void TryOpen(Player player, Follower follower)
        {
            Collidable = false;
            if (!Scene.CollideCheck<Solid>(player.Center, Center))
            {
                opening = true;
                (follower.Entity as Key).StartedUsing = true;
                Add(new Coroutine(UnlockRoutine(follower), true));
            }
            Collidable = true;
        }

        private IEnumerator UnlockRoutine(Follower follower)
        {
            SoundEmitter emitter = SoundEmitter.Play("event:/game/05_mirror_temple/key_unlock_light", this, null);
            emitter.Source.DisposeOnTransition = true;
            Level level = SceneAs<Level>();
            Key key = follower.Entity as Key;
            Add(new Coroutine(key.UseRoutine(Position + new Vector2(-5f, -7f)), true));
            yield return 1.2f;
            level.Session.DoNotLoad.Add(ID);
            VitModule.Session.introCarsToReset.Add(ID);
            key.RegisterUsed();
            yield return 0.3f;
            yield return bodySprite.PlayRoutine("open", false);
            bodySprite.Play("openIdle");
            while (key.Turning)
            {
                yield return null;
            }
            Tag |= Tags.TransitionUpdate;
            Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            level.Shake(0.3f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            wheelSprite.Play("burst");
            CustomWindController wind = Scene.Tracker.GetEntity<CustomWindController>();
            if (wind != null && wind.activateType == "Locked Door")
            {
                wind.ActivateWind();
            }
            yield return bodySprite.PlayRoutine("burst", false);
            RemoveSelf();
            wheels.RemoveSelf();
            yield break;
        }

        public IEnumerator KeyberryUnlock()
        {
            SoundEmitter emitter = SoundEmitter.Play("event:/game/05_mirror_temple/key_unlock_light", this, null);
            emitter.Source.DisposeOnTransition = true;
            Level level = SceneAs<Level>();
            yield return 0.15f;
            level.Session.DoNotLoad.Add(ID);
            VitModule.Session.introCarsToReset.Add(ID);
            Tag |= Tags.TransitionUpdate;
            Collidable = false;
            emitter.Source.DisposeOnTransition = false;
            level.Shake(0.3f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            wheelSprite.Play("burst");
            yield return bodySprite.PlayRoutine("burst", false);
            RemoveSelf();
            wheels.RemoveSelf();
            yield break;
        }

        private EntityID ID;

        private bool opening;

        private bool didHaveRider;

        private float startY;

        public Sprite bodySprite;

        public Entity wheels;

        public Sprite wheelSprite;
    }
}

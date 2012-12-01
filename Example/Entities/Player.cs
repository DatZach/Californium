﻿using System;
using System.Collections.Generic;
using Californium;
using SFML.Graphics;
using SFML.Window;

namespace Example.Entities
{
    class Player : Entity
    {
        private const int SpriteSize = 8;

        private Sprite sprite;

        private float hSave, vSave;
        private bool keyW, keyA, keyS, keyD;

        private float hSpeed, vSpeed;
        private bool canJump;
        private float jumpEnergy;
        private bool fallThrough;

        public Player(Vector2f position)
        {
            Solid = true;
            Position = position;
            Origin = new Vector2f(SpriteSize / 2, SpriteSize / 2);
            Size = new Vector2f(SpriteSize, SpriteSize);

            sprite = new Sprite(Assets.LoadTexture("Player.png")) { Origin = Origin };

            Input.Key[Keyboard.Key.W] = args =>
            {
                keyW = args.Pressed;
                if (args.Pressed)
                    canJump = true;
                else
                    jumpEnergy = 0;
                return true;
            };

            Input.Key[Keyboard.Key.A] = args => { keyA = args.Pressed; return true; };
            Input.Key[Keyboard.Key.S] = args => { keyS = args.Pressed; return true; };
            Input.Key[Keyboard.Key.D] = args => { keyD = args.Pressed; return true; };
        }

        public override void Update()
        {
            const float maxHSpeed = 2.5f;
            const float maxVSpeed = 6;
            const float acceleration = 1;
            const float jumpPotential = 10;
            const float jumpSpeed = 4;
            const float gravity = 0.7f;
            const float friction = 0.7f;

            if (keyA) hSpeed -= acceleration;
            if (keyD) hSpeed += acceleration;

            hSpeed *= friction;
            hSpeed = Utility.Clamp(hSpeed, -maxHSpeed, maxHSpeed);

            vSpeed += gravity;

            fallThrough = false;
            var bounds = BoundingBox;
            bounds.Top++;
            bool onGround = !PlaceFree(bounds);
            fallThrough = keyS && onGround;

            if (canJump)
            {
                if (onGround)
                    jumpEnergy = jumpPotential;
                canJump = false;
            }

            if (keyW && jumpEnergy > 0)
            {
                float usedEnergy = Math.Min(jumpSpeed + vSpeed, jumpEnergy);
                jumpEnergy -= usedEnergy;
                vSpeed -= usedEnergy;
            }

            vSpeed = Utility.Clamp(vSpeed, -maxVSpeed, maxVSpeed);

            float hMove = hSpeed;
            float vMove = vSpeed;

            int hRep = (int)Math.Floor(Math.Abs(hMove));
            int vRep = (int)Math.Floor(Math.Abs(vMove));

            hSave += (float)(Math.Abs(hMove) - Math.Floor(Math.Abs(hMove)));
            vSave += (float)(Math.Abs(vMove) - Math.Floor(Math.Abs(vMove)));

            while (hSave >= 1)
            {
                --hSave;
                ++hRep;
            }

            while (vSave >= 1)
            {
                --vSave;
                ++vRep;
            }

            var testRect = BoundingBox;
            while (hRep-- > 0)
            {
                testRect.Left += Math.Sign(hMove);
                if (!PlaceFree(testRect))
                {
                    hSave = 0;
                    hSpeed = 0;
                    break;
                }

                Position.X += Math.Sign(hMove);
            }

            testRect = BoundingBox;
            while (vRep-- > 0)
            {
                testRect.Top += Math.Sign(vMove);
                if (!PlaceFree(testRect))
                {
                    vSave = 0;
                    vSpeed = 0;
                    break;
                }

                Position.Y += Math.Sign(vMove);
            }
        }

        public override void Draw(RenderTarget rt)
        {
            sprite.Position = Position;
            rt.Draw(sprite);
        }

        private bool PlaceFree(FloatRect r)
        {
            TileMap.TileCollisionCondition cond = (tile, bounds, collisionBounds) =>
            {
                if (tile.UserData == null)
                    return true;
                if (fallThrough)
                    return false;
                if (vSpeed > 0 && collisionBounds.Top + collisionBounds.Height - 1 <= bounds.Top)
                    return true;
                return false;
            };

            return Parent.Map.PlaceFree(r, cond);
        }
    }
}

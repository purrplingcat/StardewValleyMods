﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Familiars
{
	public class DustSpiritFamiliar : DustSpirit
	{
		public DustSpiritFamiliar()
		{
		}


		public DustSpiritFamiliar(Vector2 position, Farmer owner) : base(position)
		{
			this.owner = owner;
			IsWalkingTowardPlayer = false;
			Sprite.interval = 45f;
			Scale = (float)Game1.random.Next(75, 101) / 100f;
			voice = (byte)Game1.random.Next(1, 24);
			HideShadow = true;
			DamageToFarmer = 0;
			moveTowardPlayerThreshold.Value = 20;
			farmerPassesThrough = true;
			reloadSprite();
		}
		public override void reloadSprite()
		{
			if (this.Sprite == null)
			{
				this.Sprite = new AnimatedSprite(ModEntry.Config.DustTexture);
			}
			else
			{
				this.Sprite.textureName.Value = ModEntry.Config.DustTexture;
			}
			if (!ModEntry.Config.DefaultDinoColor)
			{

				typeof(AnimatedSprite).GetField("spriteTexture", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Sprite, Utils.ColorFamiliar(Sprite.Texture, ModEntry.Config.DustMainColor, ModEntry.Config.DustRedColor, ModEntry.Config.DustGreenColor, ModEntry.Config.DustBlueColor));
			}
		}

		protected override void sharedDeathAnimation()
		{
		}
		public override void shedChunks(int number, float scale)
		{
			Game1.createRadialDebris(base.currentLocation, this.Sprite.textureName, new Rectangle(0, 16, 16, 16), 8, this.GetBoundingBox().Center.X, this.GetBoundingBox().Center.Y, number, (int)base.getTileLocation().Y, Color.White, (base.Health <= 0) ? 4f : 2f);
		}


		public void offScreenBehavior(Character c, GameLocation l)
		{
		}


		public override void behaviorAtGameTick(GameTime time)
		{
			invincibleCountdown = 1000;

			if (this.timeBeforeAIMovementAgain > 0f)
			{
				this.timeBeforeAIMovementAgain -= (float)time.ElapsedGameTime.Milliseconds;
			}

			if (this.yJumpOffset == 0)
			{
				if (Game1.random.NextDouble() < 0.01)
				{
					ModEntry.SHelper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue().broadcastSprites(base.currentLocation, new TemporaryAnimatedSprite[]
					{
						new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 128, 64, 64), 40f, 4, 0, base.getStandingPosition(), false, false)
					});
					foreach (Vector2 v in Utility.getAdjacentTileLocations(base.getTileLocation()))
					{
						if (base.currentLocation.objects.ContainsKey(v) && base.currentLocation.objects[v].Name.Contains("Stone"))
						{
							base.currentLocation.destroyObject(v, null);
						}
					}
					this.yJumpVelocity *= 2f;
				}
				if (!this.chargingFarmer && !chargingMonster)
				{
					this.xVelocity = (float)Game1.random.Next(-20, 21) / 5f;
				}
			}
			if (lastHitCounter >= 0)
			{
				lastHitCounter.Value -= time.ElapsedGameTime.Milliseconds;
			}

			chargingMonster = false;
			if(lastHitCounter < 0)
            {
				foreach (NPC npc in currentLocation.characters)
				{
					if (ModEntry.familiarTypes.Contains(npc.GetType()))
						continue;
					if (npc is Monster && Utils.monstersColliding(this, (Monster)npc) && Game1.random.NextDouble() < ModEntry.Config.DustStealChance)
					{
						ModEntry.SMonitor.Log("Stealing loot");
						Utils.monsterDrop(this, (Monster)npc, owner);
						lastHitCounter.Value = ModEntry.Config.DustStealInterval;
						chargingMonster = false;
						break;
					}
					else if (npc is Monster && Utils.withinMonsterThreshold(this, (Monster)npc, 5))
					{
						chargingMonster = true;
						if (currentTarget == null || Vector2.Distance(npc.position, position) < Vector2.Distance(currentTarget.position, position))
						{
							currentTarget = (Monster)npc;
						}
					}
				}
			}

			if (chargingMonster && currentTarget != null)
            {
				base.Slipperiness = 10;

				Vector2 v2 = Utils.getAwayFromNPCTrajectory(GetBoundingBox(), currentTarget);
				this.xVelocity += -v2.X / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
				if (Math.Abs(this.xVelocity) > 5f)
				{
					this.xVelocity = (float)(Math.Sign(this.xVelocity) * 5);
				}
				this.yVelocity += -v2.Y / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
				if (Math.Abs(this.yVelocity) > 5f)
				{
					this.yVelocity = (float)(Math.Sign(this.yVelocity) * 5);
				}
				return;
            }

			chargingFarmer = false;

			if (!this.seenFarmer && Utility.doesPointHaveLineOfSightInMine(base.currentLocation, base.getStandingPosition() / 64f, owner.getStandingPosition() / 64f, 8))
			{
				this.seenFarmer = true;
				return;
			}
			if (this.seenFarmer && this.controller == null && !this.runningAwayFromFarmer)
			{
				base.addedSpeed = 2;
				this.controller = new PathFindController(this, base.currentLocation, new PathFindController.isAtEnd(Utility.isOffScreenEndFunction), -1, false, new PathFindController.endBehavior(this.offScreenBehavior), 350, Point.Zero, true);
				this.runningAwayFromFarmer = true;
				return;
			}
			if (this.controller == null && this.runningAwayFromFarmer)
			{
				this.chargingFarmer = true;
			}

			if (this.chargingFarmer)
			{
				base.Slipperiness = 10;
				Vector2 v2 = Utility.getAwayFromPlayerTrajectory(this.GetBoundingBox(), owner);
				this.xVelocity += -v2.X / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
				if (Math.Abs(this.xVelocity) > 5f)
				{
					this.xVelocity = (float)(Math.Sign(this.xVelocity) * 5);
				}
				this.yVelocity += -v2.Y / 150f + ((Game1.random.NextDouble() < 0.01) ? ((float)Game1.random.Next(-50, 50) / 10f) : 0f);
				if (Math.Abs(this.yVelocity) > 5f)
				{
					this.yVelocity = (float)(Math.Sign(this.yVelocity) * 5);
				}
				if (Game1.random.NextDouble() < 0.0001)
				{
					this.controller = new PathFindController(this, base.currentLocation, new Point((int)owner.getTileLocation().X, (int)owner.getTileLocation().Y), Game1.random.Next(4), null, 300);
					this.chargingFarmer = false;
					return;
				}
			}
		}

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
			if(who != null)
            {
				return 0;
            }
			return base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
		}

		public override bool withinPlayerThreshold()
		{
			if (base.currentLocation != null && !base.currentLocation.farmers.Any())
			{
				return false;
			}
			Vector2 tileLocationOfPlayer = owner.getTileLocation();
			Vector2 tileLocationOfMonster = base.getTileLocation();
			return Math.Abs(tileLocationOfMonster.X - tileLocationOfPlayer.X) <= (float)moveTowardPlayerThreshold && Math.Abs(tileLocationOfMonster.Y - tileLocationOfPlayer.Y) <= (float)moveTowardPlayerThreshold;
		}

		protected override void initNetFields()
		{
			base.initNetFields();
			NetFields.AddFields(new INetSerializable[]
			{
				lastHitCounter,
			});
		}
		private readonly NetInt lastHitCounter = new NetInt(0);

		public Monster currentTarget = null;
		public bool followingPlayer = true;
		public Farmer owner;
		private bool chargingMonster;
		private Color color;
		private bool seenFarmer;
		private bool runningAwayFromFarmer;
		private bool chargingFarmer;
    }
}
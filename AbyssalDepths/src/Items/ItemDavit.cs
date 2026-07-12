using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

#nullable disable

namespace AbyssalDepths.src.Items
{
    public class ItemDavit : Item, IAttachableToEntity, IAttachedInteractions
    {
        CompositeShape loweredShape;

        protected ClothManager clothManager;
        protected Vec3f offsetToDavitTp = null!;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            clothManager = api.ModLoader.GetModSystem<ClothManager>();
            loweredShape = Attributes["loweredDavitShape"].AsObject<CompositeShape>();
            offsetToDavitTp = new Vec3f(0, 0, 0);
        }

        public CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
        {
            return stack.Attributes.GetBool("lowered") ? loweredShape : Shape;
        }

        public void OnInteract(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
        {
            if (onEntity.World.Side != EnumAppSide.Server)
            {
                return;
            }

            EntityControls controls = byEntity.MountedOn?.Controls ?? byEntity.Controls;
            if (mode == EnumInteractMode.Interact && controls.CtrlKey)
            {
                return;
            }

            if (itemslot.Itemstack.Attributes.GetBool("lowered"))
            {
                RaiseRope(itemslot, byEntity, onEntity);
            }
            else
            {
                LowerRope(itemslot, byEntity, onEntity);
            }
        }

        // if just a rope, then extend a rope from the davit
        private void LowerRope(ItemSlot itemslot, EntityAgent byEntity, Entity onEntity)
        {
            if (byEntity.ActiveHandItemSlot.Itemstack?.Collectible.Code.Path != "rope")
            {
                return;
            }
            byEntity.ActiveHandItemSlot.TakeOut(1);
            byEntity.ActiveHandItemSlot.MarkDirty();

            itemslot.Itemstack.Attributes.SetBool("lowered", true);
            CreateDavitRope(itemslot, (EntityBoat)onEntity);

            onEntity.MarkShapeModified();
        }

        private void RaiseRope(ItemSlot itemslot, EntityAgent byEntity, Entity onEntity)
        {
            itemslot.Itemstack.Attributes.SetBool("lowered", false);
            byEntity.TryGiveItemStack(new ItemStack(byEntity.World.GetItem(new AssetLocation("rope"))));
            clothManager.UnregisterCloth(itemslot.Itemstack.Attributes.GetInt("clothId", 0));
            itemslot.Itemstack.Attributes.RemoveAttribute("clothId");

            onEntity.MarkShapeModified();
        }

        // TODO: if a rope and diving bell is in inv then deploy both but attach the diving bell to the rope
        private void LowerDivingBell(ItemSlot itemslot, EntityAgent byEntity, Entity onEntity)
        {

        }

        // TODO: remove diving bell and rope if diving bell is pulled all the way up
        private void RaiseDivingBell(ItemSlot itemslot, EntityAgent byEntity, Entity onEntity)
        {
        }

        private ClothSystem CreateDavitRope(ItemSlot slot, EntityBoat boat)
        {
            Vec3d startPos = boat.Pos.XYZ;
            Vec3d endPos = startPos.Clone().Add(0, -8, 0);
            ClothSystem clothSystem = ClothSystem.CreateRope(api, clothManager, startPos, endPos, null, 4f);
            clothSystem.CanPull = false;
            clothSystem.CanRip = false;
            clothSystem.FirstPoint.PinTo(boat, offsetToDavitTp);
            clothSystem.LastPoint.NoAttachTransform = true;
            clothSystem.RopeRenderThickness = 1f;
            clothManager.RegisterCloth(clothSystem);
            slot.Itemstack?.Attributes.SetInt("clothId", clothSystem.ClothId);
            slot.MarkDirty();
            return clothSystem;
        }

        public int RequiresBehindSlots { get; set; } = 0;
        public string GetCategoryCode(ItemStack stack) => "davit";
        public string[] GetDisableElements(ItemStack stack) => Array.Empty<string>();
        public string[] GetKeepElements(ItemStack stack) => Array.Empty<string>();
        public string GetTexturePrefixCode(ItemStack stack) => "davit";
        public bool IsAttachable(Entity toEntity, ItemStack itemStack) => true;
        public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict) { }

        public void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity)
        {
            itemslot.Itemstack.Attributes.RemoveAttribute("lowered");
        }

        // TODO: when the davit is removed from the boat, the rope or diving bell is also removed
        public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity)
        {
            itemslot.Itemstack.Attributes.RemoveAttribute("lowered");
            clothManager.UnregisterCloth(itemslot.Itemstack.Attributes.GetInt("clothId", 0));
            itemslot.Itemstack.Attributes.RemoveAttribute("clothId");
        }

        public void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath)
        {
            clothManager.UnregisterCloth(itemslot.Itemstack.Attributes.GetInt("clothId", 0));
            itemslot.Itemstack.Attributes.RemoveAttribute("clothId");
        }

        public void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn)
        {
            clothManager.UnregisterCloth(itemslot.Itemstack.Attributes.GetInt("clothId", 0));
            itemslot.Itemstack.Attributes.RemoveAttribute("clothId");
        }

        public void OnReceivedClientPacket(ItemSlot itemslot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled, Action onRequireSave) { }

        public bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity) { return true; }

        public bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity toEntity) { return true; }
    }
}
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace AbyssalDepths.src.Items
{
    public class ItemDavit : Item, IAttachableToEntity, IAttachedInteractions
    {

        public CompositeShape GetAttachedShape(ItemStack stack, string slotCode) => Shape;

        public void OnInteract(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
        {
            // on interact with a diving bell, attach the diving bell to the davit if possible
            onEntity.MarkShapeModified();
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
            // davit should never have a diving bell attched when its not placed
        }

        public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity)
        {
            // on detach remove the attached diving bell if there is one
        }

        public void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath) { }

        public void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn) { }


        public void OnReceivedClientPacket(ItemSlot itemslot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled, Action onRequireSave) { }

        public bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity) { return true; }

        public bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity toEntity) { return true; }
    }
}

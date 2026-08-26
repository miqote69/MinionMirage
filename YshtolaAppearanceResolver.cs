using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace MinionToNPC;

internal static class TargetAppearanceResolver
{
    public static AppearancePayload Resolve(IDataManager dataManager, PrototypeMapping mapping)
        => mapping.TargetKind switch
        {
            PrototypeTargetKind.EventNpc => ResolveEventNpc(dataManager, mapping),
            PrototypeTargetKind.BattleNpc => ResolveBattleNpc(dataManager, mapping),
            _ => throw new InvalidOperationException($"Unsupported target kind: {mapping.TargetKind}"),
        };

    private static AppearancePayload ResolveEventNpc(IDataManager dataManager, PrototypeMapping mapping)
    {
        var sheet = dataManager.GetExcelSheet<ENpcBase>();
        if (!sheet.TryGetRow(mapping.TargetRowId, out var row))
            throw new InvalidOperationException(
                $"ENpcBase row {mapping.TargetRowId} is unavailable.");

        var model = row.ModelChara.ValueNullable
            ?? throw new InvalidOperationException(
                $"ModelChara row {row.ModelChara.RowId} is unavailable for ENpcBase {row.RowId}.");
        if (row.ModelChara.RowId != mapping.TargetModelCharaRowId)
            throw new InvalidOperationException(
                $"ENpcBase {row.RowId} resolved ModelChara {row.ModelChara.RowId}, expected {mapping.TargetModelCharaRowId}.");
        if (mapping.IsHuman && model.Type != 1)
            throw new InvalidOperationException(
                $"ENpcBase {row.RowId} does not resolve to a Human ModelChara.");
        if (!mapping.IsHuman && model.Type == 3)
            return new AppearancePayload(
                row.ModelChara.RowId,
                new byte[26],
                new ulong[10],
                IsHuman: false);
        if (!mapping.IsHuman && model.Type != 2)
            throw new InvalidOperationException(
                $"ENpcBase {row.RowId} does not resolve to a DemiHuman ModelChara.");

        var customize = CreateCustomize(row);

        var equipment = row.NpcEquip.RowId is not 0
            && row.NpcEquip.ValueNullable is { } referenced
            && row is { ModelBody: 0, ModelLegs: 0 }
                ? CreateEquipment(referenced)
                : CreateEquipment(row);

        return new AppearancePayload(row.ModelChara.RowId, customize, equipment, mapping.IsHuman);
    }

    private static AppearancePayload ResolveBattleNpc(IDataManager dataManager, PrototypeMapping mapping)
    {
        var sheet = dataManager.GetExcelSheet<BNpcBase>();
        if (!sheet.TryGetRow(mapping.TargetRowId, out var row))
            throw new InvalidOperationException(
                $"BNpcBase row {mapping.TargetRowId} is unavailable.");

        var model = row.ModelChara.ValueNullable
            ?? throw new InvalidOperationException(
                $"ModelChara row {row.ModelChara.RowId} is unavailable for BNpcBase {row.RowId}.");
        if (mapping.IsHuman)
        {
            if (model.Type != 1)
                throw new InvalidOperationException(
                    $"BNpcBase {row.RowId} does not resolve to a Human ModelChara.");
            if (row.ModelChara.RowId != mapping.TargetModelCharaRowId)
                throw new InvalidOperationException(
                    $"BNpcBase {row.RowId} resolved ModelChara {row.ModelChara.RowId}, expected {mapping.TargetModelCharaRowId}.");

            var customize = row.BNpcCustomize.ValueNullable
                ?? throw new InvalidOperationException(
                    $"BNpcCustomize row {row.BNpcCustomize.RowId} is unavailable for BNpcBase {row.RowId}.");
            var equipment = row.NpcEquip.ValueNullable
                ?? throw new InvalidOperationException(
                    $"NpcEquip row {row.NpcEquip.RowId} is unavailable for BNpcBase {row.RowId}.");

            return new AppearancePayload(
                row.ModelChara.RowId,
                CreateCustomize(customize),
                CreateEquipment(equipment),
                IsHuman: true);
        }

        if (model.Type == 1)
            throw new InvalidOperationException(
                $"BNpcBase {row.RowId} does not resolve to a non-Human ModelChara.");
        if (row.ModelChara.RowId != mapping.TargetModelCharaRowId)
            throw new InvalidOperationException(
                $"BNpcBase {row.RowId} resolved ModelChara {row.ModelChara.RowId}, expected {mapping.TargetModelCharaRowId}.");

        if (model.Type == 2)
        {
            var customize = row.BNpcCustomize.ValueNullable
                ?? throw new InvalidOperationException(
                    $"BNpcCustomize row {row.BNpcCustomize.RowId} is unavailable for DemiHuman BNpcBase {row.RowId}.");
            var equipment = row.NpcEquip.ValueNullable
                ?? throw new InvalidOperationException(
                    $"NpcEquip row {row.NpcEquip.RowId} is unavailable for DemiHuman BNpcBase {row.RowId}.");

            return new AppearancePayload(
                row.ModelChara.RowId,
                CreateCustomize(customize),
                CreateEquipment(equipment),
                IsHuman: false);
        }

        if (model.Type != 3)
            throw new InvalidOperationException(
                $"BNpcBase {row.RowId} resolves unsupported non-Human ModelChara type {model.Type}.");

        return new AppearancePayload(
            row.ModelChara.RowId,
            new byte[26],
            new ulong[10],
            IsHuman: false);
    }

    private static byte[] CreateCustomize(ENpcBase row)
        =>
        [
            checked((byte)row.Race.RowId),
            checked((byte)row.Gender),
            row.BodyType,
            row.Height,
            checked((byte)row.Tribe.RowId),
            row.Face,
            row.HairStyle,
            row.HairHighlight,
            row.SkinColor,
            row.EyeHeterochromia,
            row.HairColor,
            row.HairHighlightColor,
            row.FacialFeature,
            row.FacialFeatureColor,
            row.Eyebrows,
            row.EyeColor,
            row.EyeShape,
            row.Nose,
            row.Jaw,
            row.Mouth,
            row.LipColor,
            row.BustOrTone1,
            row.ExtraFeature1,
            row.ExtraFeature2OrBust,
            row.FacePaint,
            row.FacePaintColor,
        ];

    private static byte[] CreateCustomize(BNpcCustomize row)
        =>
        [
            checked((byte)row.Race.RowId),
            checked((byte)row.Gender),
            row.BodyType,
            row.Height,
            checked((byte)row.Tribe.RowId),
            row.Face,
            row.HairStyle,
            row.HairHighlight,
            row.SkinColor,
            row.EyeHeterochromia,
            row.HairColor,
            row.HairHighlightColor,
            row.FacialFeature,
            row.FacialFeatureColor,
            row.Eyebrows,
            row.EyeColor,
            row.EyeShape,
            row.Nose,
            row.Jaw,
            row.Mouth,
            row.LipColor,
            row.BustOrTone1,
            row.ExtraFeature1,
            row.ExtraFeature2OrBust,
            row.FacePaint,
            row.FacePaintColor,
        ];

    private static ulong[] CreateEquipment(ENpcBase row)
        =>
        [
            PackArmor(row.ModelHead, row.DyeHead.RowId, row.Dye2Head.RowId),
            PackArmor(row.ModelBody, row.DyeBody.RowId, row.Dye2Body.RowId),
            PackArmor(row.ModelHands, row.DyeHands.RowId, row.Dye2Hands.RowId),
            PackArmor(row.ModelLegs, row.DyeLegs.RowId, row.Dye2Legs.RowId),
            PackArmor(row.ModelFeet, row.DyeFeet.RowId, row.Dye2Feet.RowId),
            PackArmor(row.ModelEars, row.DyeEars.RowId, row.Dye2Ears.RowId),
            PackArmor(row.ModelNeck, row.DyeNeck.RowId, row.Dye2Neck.RowId),
            PackArmor(row.ModelWrists, row.DyeWrists.RowId, row.Dye2Wrists.RowId),
            PackArmor(row.ModelRightRing, row.DyeRightRing.RowId, row.Dye2RightRing.RowId),
            PackArmor(row.ModelLeftRing, row.DyeLeftRing.RowId, row.Dye2LeftRing.RowId),
        ];

    private static ulong[] CreateEquipment(NpcEquip row)
        =>
        [
            PackArmor(row.ModelHead, row.DyeHead.RowId, row.Dye2Head.RowId),
            PackArmor(row.ModelBody, row.DyeBody.RowId, row.Dye2Body.RowId),
            PackArmor(row.ModelHands, row.DyeHands.RowId, row.Dye2Hands.RowId),
            PackArmor(row.ModelLegs, row.DyeLegs.RowId, row.Dye2Legs.RowId),
            PackArmor(row.ModelFeet, row.DyeFeet.RowId, row.Dye2Feet.RowId),
            PackArmor(row.ModelEars, row.DyeEars.RowId, row.Dye2Ears.RowId),
            PackArmor(row.ModelNeck, row.DyeNeck.RowId, row.Dye2Neck.RowId),
            PackArmor(row.ModelWrists, row.DyeWrists.RowId, row.Dye2Wrists.RowId),
            PackArmor(row.ModelRightRing, row.DyeRightRing.RowId, row.Dye2RightRing.RowId),
            PackArmor(row.ModelLeftRing, row.DyeLeftRing.RowId, row.Dye2LeftRing.RowId),
        ];

    private static ulong PackArmor(ulong model, uint stain1, uint stain2)
        => model | ((ulong)stain1 << 24) | ((ulong)stain2 << 32);
}

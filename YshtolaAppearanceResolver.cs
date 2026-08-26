using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace MinionToNPC;

internal static class YshtolaAppearanceResolver
{
    public static AppearancePayload Resolve(IDataManager dataManager)
    {
        var sheet = dataManager.GetExcelSheet<ENpcBase>();
        if (!sheet.TryGetRow(PrototypeContract.TargetEventNpcRowId, out var row))
            throw new InvalidOperationException(
                $"ENpcBase row {PrototypeContract.TargetEventNpcRowId} is unavailable.");

        var model = row.ModelChara.ValueNullable
            ?? throw new InvalidOperationException(
                $"ModelChara row {row.ModelChara.RowId} is unavailable for ENpcBase {row.RowId}.");
        if (model.Type != 1)
            throw new InvalidOperationException(
                $"ENpcBase {row.RowId} does not resolve to a Human ModelChara.");

        var customize = new byte[]
        {
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
        };

        var equipment = row.NpcEquip.RowId is not 0
            && row.NpcEquip.ValueNullable is { } referenced
            && row is { ModelBody: 0, ModelLegs: 0 }
                ? CreateEquipment(referenced)
                : CreateEquipment(row);

        return new AppearancePayload(row.ModelChara.RowId, customize, equipment);
    }

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

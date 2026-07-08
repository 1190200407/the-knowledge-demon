using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ComicChess.KnowledgeDemon;

[RegisterCard(typeof(KnowledgeDemonCardPool))]
public sealed class MimicryCharacterOption : KnowledgeDemonCardModel
{
    private const string CharacterNameVarName = "ChosenCharacter";

    private const int EnergyCostValue = -1;
    private const CardType TypeValue = CardType.Skill;
    private const CardRarity RarityValue = CardRarity.Token;
    private const TargetType TargetTypeValue = TargetType.None;
    private const bool ShouldShowInCardLibraryValue = false;

    [SavedProperty]
    public ModelId? ChosenCharacterId { get; set; }

    [SavedProperty]
    public ModelId? PreviewCardId { get; set; }

    public override bool CanBeGeneratedInCombat => false;

    public override CardPoolModel VisualCardPool =>
        ChosenCharacterId is { } chosenCharacterId
            ? ModelDb.GetById<CharacterModel>(chosenCharacterId).CardPool
            : base.VisualCardPool;

    public override string PortraitPath
    {
        get
        {
            if (PreviewCardId is { } previewCardId && ModelDb.GetById<CardModel>(previewCardId) is { } previewCard)
            {
                return previewCard.PortraitPath;
            }

            return base.PortraitPath;
        }
    }

    public override string Title =>
        ChosenCharacterId is { } chosenCharacterId
            ? ModelDb.GetById<CharacterModel>(chosenCharacterId).Title.GetFormattedText()
            : base.Title;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar(CharacterNameVarName)];

    public MimicryCharacterOption()
        : base(EnergyCostValue, TypeValue, RarityValue, TargetTypeValue, ShouldShowInCardLibraryValue)
    {
    }

    public void SetChosenCharacter(CharacterModel character, CardModel? previewCard)
    {
        ChosenCharacterId = character.Id;
        PreviewCardId = previewCard?.Id;
        SyncCharacterNameVar();
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        _ = description;
        SyncCharacterNameVar();
    }

    private void SyncCharacterNameVar()
    {
        if (ChosenCharacterId is not { } chosenCharacterId)
        {
            return;
        }

        ((StringVar)DynamicVars[CharacterNameVarName]).StringValue =
            ModelDb.GetById<CharacterModel>(chosenCharacterId).Title.GetFormattedText();
    }
}

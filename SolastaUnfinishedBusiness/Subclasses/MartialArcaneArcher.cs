﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using static ActionDefinitions;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionActionAffinitys;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionSubclassChoices;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.ActionDefinitions;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class MartialArcaneArcher : AbstractSubclass
{
    private const string Name = "MartialArcaneArcher";
    private const string FeatureSetArcaneShotName = $"FeatureSet{Name}ArcaneShot";
    private const Id ArcaneArcherToggle = (Id)ExtraActionId.ArcaneArcherToggle;

    internal static readonly FeatureDefinitionPower PowerArcaneShot = FeatureDefinitionPowerBuilder
        .Create($"Power{Name}ArcaneShot")
        .SetGuiPresentation(FeatureSetArcaneShotName, Category.Feature)
        .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ShortRest, 1, 0)
        .DelegatedToAction()
        .AddToDB();

    internal static readonly FeatureDefinitionActionAffinity ActionAffinityArcaneArcherToggle =
        FeatureDefinitionActionAffinityBuilder
            .Create(ActionAffinitySorcererMetamagicToggle, "ActionAffinityArcaneArcherToggle")
            .SetGuiPresentationNoContent(true)
            .SetAuthorizedActions(ArcaneArcherToggle)
            .AddToDB();

    internal static readonly FeatureDefinitionCustomInvocationPool InvocationPoolArcaneShotChoice2 =
        CustomInvocationPoolDefinitionBuilder
            .Create("InvocationPoolArcaneShotChoice2")
            .SetGuiPresentation(Category.Feature)
            .Setup(InvocationPoolTypeCustom.Pools.ArcaneShotChoice, 2)
            .AddToDB();

    public MartialArcaneArcher()
    {
        // LEVEL 03

        // Arcane Lore

        var proficiencyArcana = FeatureDefinitionProficiencyBuilder
            .Create($"Proficiency{Name}Arcana")
            .SetGuiPresentation(Category.Feature)
            .SetProficiencies(ProficiencyType.Skill, SkillDefinitions.Arcana)
            .AddToDB();

        var proficiencyNature = FeatureDefinitionProficiencyBuilder
            .Create($"Proficiency{Name}Nature")
            .SetGuiPresentation(Category.Feature)
            .SetProficiencies(ProficiencyType.Skill, SkillDefinitions.Nature)
            .AddToDB();

        var featureSetArcaneLore = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ArcaneLore")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(proficiencyArcana, proficiencyNature)
            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Exclusion)
            .AddToDB();

        // Arcane Magic

        var spellListArcaneMagic = SpellListDefinitionBuilder
            .Create($"SpellList{Name}ArcaneMagic")
            .SetGuiPresentationNoContent(true)
            .FinalizeSpells()
            .AddToDB();

        //explicitly re-use wizard spell list, so custom cantrips selected for wizard will show here 
        spellListArcaneMagic.SpellsByLevel[0].Spells = SpellListDefinitions.SpellListWizard.SpellsByLevel[0].Spells;

        var castSpellArcaneMagic = FeatureDefinitionCastSpellBuilder
            .Create(FeatureDefinitionCastSpells.CastSpellElfHigh, $"CastSpell{Name}ArcaneMagic")
            .SetGuiPresentation(Category.Feature)
            .SetSpellCastingAbility(AttributeDefinitions.Intelligence)
            .SetSpellList(spellListArcaneMagic)
            .AddToDB();

        // Arcane Shot

        var arcaneShotPowers =
            BuildArcaneShotPowers(PowerArcaneShot, out var powerBurstingArrow, out var powerBurstingArrowDamage);

        PowerArcaneShot.AddCustomSubFeatures(
            HasModifiedUses.Marker,
            new CustomBehaviorArcaneShot(powerBurstingArrow, powerBurstingArrowDamage));

        PowerBundle.RegisterPowerBundle(PowerArcaneShot, false, arcaneShotPowers);

        _ = ActionDefinitionBuilder
            .Create(MetamagicToggle, "ArcaneArcherToggle")
            .SetOrUpdateGuiPresentation(FeatureSetArcaneShotName, Category.Feature)
            .RequiresAuthorization()
            .SetActionId(ExtraActionId.ArcaneArcherToggle)
            .SetActivatedPower(PowerArcaneShot)
            .OverrideClassName("Toggle")
            .AddToDB();

        var powerArcaneShotAdditionalUse2 = FeatureDefinitionPowerUseModifierBuilder
            .Create($"PowerUseModifier{Name}ArcaneShotUse2")
            .SetGuiPresentationNoContent(true)
            .SetFixedValue(PowerArcaneShot, 2)
            .AddToDB();

        var invocationPoolArcaneShotChoice1 =
            CustomInvocationPoolDefinitionBuilder
                .Create("InvocationPoolArcaneShotChoice1")
                .SetGuiPresentation(Category.Feature)
                .Setup(InvocationPoolTypeCustom.Pools.ArcaneShotChoice)
                .AddToDB();

        var featureSetArcaneShot = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ArcaneShot")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(
                ActionAffinityArcaneArcherToggle,
                InvocationPoolArcaneShotChoice2,
                powerArcaneShotAdditionalUse2,
                PowerArcaneShot)
            .AddToDB();

        // LEVEL 07

        // Magic Arrow

        var featureMagicArrow = FeatureDefinitionBuilder
            .Create($"Feature{Name}MagicArrow")
            .SetGuiPresentation(Category.Feature)
            .AddCustomSubFeatures(new AddTagToWeaponWeaponAttack(TagsDefinitions.MagicalWeapon, IsBow))
            .AddToDB();

        // Guided Shot

        var featureGuidedShot = FeatureDefinitionBuilder
            .Create($"Feature{Name}GuidedShot")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureGuidedShot.AddCustomSubFeatures(new TryAlterOutcomeAttackGuidedShot(featureGuidedShot));

        // LEVEL 10

        // Arcane Shot Additional Use

        var powerArcaneShotAdditionalUse1At10 = FeatureDefinitionPowerUseModifierBuilder
            .Create($"PowerUseModifier{Name}ArcaneShotUse1At10")
            .SetGuiPresentation($"PowerUseModifier{Name}ArcaneShotUse1", Category.Feature)
            .SetFixedValue(PowerArcaneShot, 1)
            .AddToDB();

        // Arcane Shot Choice

        // LEVEL 15

        // Ever-Ready Shot

        var featureEverReadyShot = FeatureDefinitionBuilder
            .Create($"Feature{Name}EverReadyShot")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureEverReadyShot.AddCustomSubFeatures(new BattleStartedListenerEverReadyShot(featureEverReadyShot));

        // LEVEL 18

        // Arcane Shot Additional Use

        var powerArcaneShotAdditionalUse1At18 = FeatureDefinitionPowerUseModifierBuilder
            .Create($"PowerUseModifier{Name}ArcaneShotUse1At18")
            .SetGuiPresentation($"PowerUseModifier{Name}ArcaneShotUse1", Category.Feature)
            .SetFixedValue(PowerArcaneShot, 1)
            .AddToDB();

        // Arcane Shot Choice

        // MAIN

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, Sprites.GetSprite(Name, Resources.MartialArcaneArcher, 256))
            .AddFeaturesAtLevel(3,
                featureSetArcaneLore,
                castSpellArcaneMagic,
                featureSetArcaneShot)
            .AddFeaturesAtLevel(7,
                featureMagicArrow,
                featureGuidedShot,
                invocationPoolArcaneShotChoice1)
            .AddFeaturesAtLevel(10,
                powerArcaneShotAdditionalUse1At10,
                invocationPoolArcaneShotChoice1)
            .AddFeaturesAtLevel(15,
                featureEverReadyShot,
                invocationPoolArcaneShotChoice1)
            .AddFeaturesAtLevel(18,
                powerArcaneShotAdditionalUse1At18,
                invocationPoolArcaneShotChoice1)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Fighter;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice => SubclassChoiceFighterMartialArchetypes;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    private static IsWeaponValidHandler IsBow =>
        ValidatorsWeapon.IsOfWeaponType(WeaponTypeDefinitions.LongbowType, WeaponTypeDefinitions.ShortbowType);

    private static List<FeatureDefinitionPower> BuildArcaneShotPowers(
        FeatureDefinitionPower pool,
        out FeatureDefinitionPower powerBurstingArrow,
        out FeatureDefinitionPower powerBurstingArrowDamage)
    {
        var powers = new List<FeatureDefinitionPower>();

        // Banishing Arrow

        var powerBanishingArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}BanishingArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Banishment, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.Individuals)
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetParticleEffectParameters(SpellDefinitions.Banishment)
                    .SetSavingThrowData(
                        false, AttributeDefinitions.Charisma, false,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Intelligence,
                        8)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(
                                ConditionDefinitions.ConditionBanished, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(
                        SpellDefinitions.Banishment.EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        powers.Add(powerBanishingArrow);

        // Beguiling Arrow

        var powerBeguilingArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}BeguilingArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.CharmPerson, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.Individuals)
                    .SetDurationData(DurationType.Round, 1)
                    .SetParticleEffectParameters(SpellDefinitions.CharmPerson)
                    .SetSavingThrowData(
                        false, AttributeDefinitions.Wisdom, false,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Intelligence,
                        8)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypePsychic, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(
                                ConditionDefinitions.ConditionCharmed, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(
                        SpellDefinitions.CharmPerson.EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        powers.Add(powerBeguilingArrow);

        // Bursting Arrow

        powerBurstingArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}BurstingArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.EldritchBlast, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .Build())
            .AddToDB();

        powerBurstingArrowDamage = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}BurstingArrowDamage")
            .SetGuiPresentation($"Power{Name}BurstingArrow", Category.Feature, SpellDefinitions.EldritchBlast,
                hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build())
                    .SetImpactEffectParameters(SpellDefinitions.BurningHands_B)
                    .Build())
            .AddToDB();

        powers.Add(powerBurstingArrow);

        // Enfeebling Arrow

        var abilityCheckAffinityEnfeeblingArrow = FeatureDefinitionAbilityCheckAffinityBuilder
            .Create($"AbilityCheckAffinity{Name}EnfeeblingArrow")
            .SetGuiPresentation($"Condition{Name}EnfeeblingArrow", Category.Condition, Gui.NoLocalization)
            .BuildAndSetAffinityGroups(CharacterAbilityCheckAffinity.Disadvantage,
                AttributeDefinitions.Strength,
                AttributeDefinitions.Dexterity,
                AttributeDefinitions.Constitution)
            .AddToDB();

        var savingThrowAffinityEnfeeblingArrow = FeatureDefinitionSavingThrowAffinityBuilder
            .Create($"SavingThrowAffinity{Name}EnfeeblingArrow")
            .SetGuiPresentation($"Condition{Name}EnfeeblingArrow", Category.Condition, Gui.NoLocalization)
            .SetAffinities(CharacterSavingThrowAffinity.Disadvantage, false,
                AttributeDefinitions.Strength,
                AttributeDefinitions.Dexterity,
                AttributeDefinitions.Constitution)
            .AddToDB();

        var conditionEnfeeblingArrow = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionEnfeebled, $"Condition{Name}EnfeeblingArrow")
            .SetOrUpdateGuiPresentation(Category.Condition)
            .SetParentCondition(ConditionDefinitions.ConditionEnfeebled)
            .AddFeatures(
                abilityCheckAffinityEnfeeblingArrow,
                savingThrowAffinityEnfeeblingArrow)
            .AddToDB();

        var powerEnfeeblingArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}EnfeeblingArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.RayOfEnfeeblement, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.Individuals)
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetParticleEffectParameters(SpellDefinitions.RayOfEnfeeblement)
                    .SetSavingThrowData(
                        false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Intelligence,
                        8)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeNecrotic, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(
                                conditionEnfeeblingArrow, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(
                        SpellDefinitions.RayOfEnfeeblement.EffectDescription.EffectParticleParameters
                            .effectParticleReference)
                    .Build())
            .AddToDB();

        powers.Add(powerEnfeeblingArrow);

        // Grasping Arrow

        var conditionGraspingArrow = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionRestrained, $"Condition{Name}GraspingArrow")
            .SetParentCondition(ConditionDefinitions.ConditionRestrained)
            .SetFeatures()
            .SetConditionParticleReference(ConditionDefinitions.ConditionRestrainedByMagicalArrow)
            .AddToDB();

        var powerGraspingArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}GraspingArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Entangle, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.Individuals)
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetParticleEffectParameters(SpellDefinitions.Entangle)
                    .SetSavingThrowData(
                        false, AttributeDefinitions.Strength, false,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Intelligence,
                        8)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeSlashing, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(
                                conditionGraspingArrow, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(
                        SpellDefinitions.Entangle.EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        powers.Add(powerGraspingArrow);

        // Insight Arrow

        var lightSourceForm = SpellDefinitions.FaerieFire.EffectDescription
            .GetFirstFormOfType(EffectForm.EffectFormType.LightSource).LightSourceForm;

        var conditionInsightArrow = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionHighlighted, $"Condition{Name}InsightArrow")
            .SetOrUpdateGuiPresentation(Category.Condition)
            .SetConditionParticleReference(ConditionDefinitions.ConditionShine)
            .AddToDB();

        var powerInsightArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}InsightArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.TrueStrike, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(SpellDefinitions.FaerieFire)
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.Individuals)
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetParticleEffectParameters(SpellDefinitions.FaerieFire)
                    .SetSavingThrowData(
                        false, AttributeDefinitions.Dexterity, false,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Intelligence,
                        8)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeRadiant, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetLightSourceForm(
                                LightSourceType.Basic, 2, 2, lightSourceForm.Color,
                                lightSourceForm.graphicsPrefabReference)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(
                                conditionInsightArrow, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(
                        SpellDefinitions.Shine.EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        powers.Add(powerInsightArrow);

        // Shadow Arrow

        var powerShadowArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}ShadowArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Blindness, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.Individuals)
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetParticleEffectParameters(SpellDefinitions.Blindness)
                    .SetSavingThrowData(
                        false, AttributeDefinitions.Wisdom, false,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Intelligence,
                        8)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypePsychic, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(
                                ConditionDefinitions.ConditionBlinded, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(
                        SpellDefinitions.Blindness.EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        powers.Add(powerShadowArrow);

        // Slowing Arrow

        var powerSlowingArrow = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}SlowingArrow")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Slow, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, pool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 1, TargetType.Individuals)
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetParticleEffectParameters(SpellDefinitions.Slow)
                    .SetSavingThrowData(
                        false, AttributeDefinitions.Dexterity, false,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Intelligence,
                        8)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, 2, DieType.D6)
                            .SetDiceAdvancement(LevelSourceType.ClassLevel, 1, 1, 6, 11)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(
                                ConditionDefinitions.ConditionSlowed, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(
                        SpellDefinitions.Slow.EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        powers.Add(powerSlowingArrow);

        // create UI choices

        foreach (var power in powers)
        {
            var name = power.Name.Replace("Power", string.Empty);
            var guiPresentation = power.guiPresentation;

            _ = CustomInvocationDefinitionBuilder
                .Create($"CustomInvocation{name}")
                .SetGuiPresentation(guiPresentation)
                .SetPoolType(InvocationPoolTypeCustom.Pools.ArcaneShotChoice)
                .SetGrantedFeature(power)
                .AddCustomSubFeatures(ModifyInvocationVisibility.Marker)
                .AddToDB();
        }

        return powers;
    }

    //
    // Arcane Shot
    //

    private sealed class CustomBehaviorArcaneShot(
        FeatureDefinitionPower powerBurstingArrow,
        FeatureDefinitionPower powerBurstingArrowDamage)
        : IPhysicalAttackBeforeHitConfirmedOnEnemy, IPhysicalAttackFinishedByMe
    {
        private const string ArcaneShotMarker = "ArcaneShot";

        public IEnumerator OnPhysicalAttackBeforeHitConfirmedOnEnemy(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            bool rangedAttack,
            AdvantageType advantageType,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(PowerArcaneShot, rulesetAttacker);

            if (!attacker.OnceInMyTurnIsValid(ArcaneShotMarker) ||
                !attacker.RulesetCharacter.IsToggleEnabled(ArcaneArcherToggle) ||
                !IsBow(attackMode, null, null) ||
                rulesetAttacker.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            yield return attacker.MyReactToSpendPowerBundle(
                usablePower,
                [defender],
                attacker,
                "ArcaneShot",
                reactionValidated: ReactionValidated,
                battleManager: battleManager);

            yield break;

            void ReactionValidated(ReactionRequestSpendBundlePower reactionRequest)
            {
                attacker.UsedSpecialFeatures.TryAdd(powerBurstingArrow.Name, 0);
                attacker.UsedSpecialFeatures[powerBurstingArrow.Name] = -1;
                attacker.UsedSpecialFeatures.TryAdd(ArcaneShotMarker, 1);

                var option = reactionRequest.SelectedSubOption;
                var subPowers = PowerArcaneShot.GetBundle()?.SubPowers;

                if (subPowers != null &&
                    subPowers[option] == powerBurstingArrow)
                {
                    attacker.UsedSpecialFeatures[powerBurstingArrow.Name] = 0;
                }
            }
        }

        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            if (!attacker.UsedSpecialFeatures.TryGetValue(powerBurstingArrow.Name, out var value) || value < 0)
            {
                yield break;
            }

            attacker.UsedSpecialFeatures[powerBurstingArrow.Name] = -1;
            HandleBurstingArrow(attacker, defender);
        }

        private void HandleBurstingArrow(GameLocationCharacter attacker, GameLocationCharacter defender)
        {
            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerBurstingArrowDamage, rulesetAttacker);
            var targets = Gui.Battle.AllContenders
                .Where(x => x.IsWithinRange(defender, 3) && x != defender)
                .ToArray();

            EffectHelpers
                .StartVisualEffect(attacker, defender, SpellDefinitions.Shatter, EffectHelpers.EffectType.Zone);

            // burst arrow damage is a use at will power
            attacker.MyExecuteActionSpendPower(usablePower, targets);
        }
    }

    //
    // Guided Shot
    //

    private class TryAlterOutcomeAttackGuidedShot(FeatureDefinition featureDefinition) : ITryAlterOutcomeAttack
    {
        public int HandlerPriority => -10;

        public IEnumerator OnTryAlterOutcomeAttack(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            ActionModifier attackModifier,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect)
        {
            if (action.AttackRollOutcome is not (RollOutcome.Failure or RollOutcome.CriticalFailure) ||
                helper != attacker ||
                !helper.CanReact() ||
                !IsBow(attackMode, null, null))
            {
                yield break;
            }

            yield return attacker.MyReactToDoNothing(
                ExtraActionId.DoNothingReaction,
                attacker,
                "MartialArcaneArcherGuidedShot",
                "CustomReactionMartialArcaneArcherGuidedShotDescription".Formatted(Category.Reaction),
                ReactionValidated,
                battleManager: battleManager);

            yield break;

            void ReactionValidated()
            {
                var toHitBonus = attackMode.ToHitBonus + attackModifier.AttackRollModifier;
                var totalRoll = (action.AttackRoll + attackMode.ToHitBonus).ToString();
                var rollCaption = action.AttackRollOutcome == RollOutcome.CriticalFailure
                    ? "Feedback/&RollAttackCriticalFailureTitle"
                    : "Feedback/&RollAttackFailureTitle";

                var rulesetAttacker = attacker.RulesetCharacter;
                var sign = toHitBonus > 0 ? "+" : string.Empty;

                rulesetAttacker.LogCharacterUsedFeature(featureDefinition,
                    "Feedback/&TriggerRerollLine",
                    false,
                    (ConsoleStyleDuplet.ParameterType.Base, $"{action.AttackRoll}{sign}{toHitBonus}"),
                    (ConsoleStyleDuplet.ParameterType.FailedRoll, Gui.Format(rollCaption, totalRoll)));

                var roll = rulesetAttacker.RollAttack(
                    attackMode.toHitBonus,
                    defender.RulesetActor,
                    attackMode.sourceDefinition,
                    attackModifier.AttacktoHitTrends,
                    attackModifier.IgnoreAdvantage,
                    attackModifier.AttackAdvantageTrends,
                    attackMode.Ranged,
                    false,
                    attackModifier.AttackRollModifier,
                    out var outcome,
                    out var successDelta,
                    -1,
                    true);

                action.AttackRollOutcome = outcome;
                action.AttackSuccessDelta = successDelta;
                action.AttackRoll = roll;
            }
        }
    }

    //
    // Ready Shot
    //

    private sealed class BattleStartedListenerEverReadyShot(FeatureDefinition featureDefinition)
        : ICharacterBattleStartedListener
    {
        public void OnCharacterBattleStarted(GameLocationCharacter locationCharacter, bool surprise)
        {
            var character = locationCharacter.RulesetCharacter;

            if (character is not { IsDeadOrDyingOrUnconscious: false })
            {
                return;
            }

            var levels = character.GetClassLevel(CharacterClassDefinitions.Fighter);

            if (levels < 15)
            {
                return;
            }

            var usablePower = PowerProvider.Get(PowerArcaneShot, character);

            if (character.GetRemainingUsesOfPower(usablePower) > 0)
            {
                return;
            }

            character.RepayPowerUse(usablePower);
            character.LogCharacterUsedFeature(featureDefinition);
        }
    }
}

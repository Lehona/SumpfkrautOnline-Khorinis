/* DROP DATABASE `SOK_Gameserver`;

CREATE DATABASE IF NOT EXISTS `SOK_Gameserver`;

USE `SOK_Gameserver`; */

DROP TABLE IF EXISTS `WorldInst`;
CREATE TABLE IF NOT EXISTS `WorldInst` (
    `WorldInstID` INTEGER  NOT NULL,
    `Name` TEXT DEFAULT NULL,
    `Path` TEXT DEFAULT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`WorldInstID`)
);

-- individual player accounts
DROP TABLE IF EXISTS `AccountInstance`;
CREATE TABLE IF NOT EXISTS `AccountInst` (
    `AccountInstID` INTEGER  NOT NULL,
    `Name` TEXT NOT NULL,
    `Password` TEXT NOT NULL,
    `EMail` TEXT DEFAULT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`AccountInst`)
);

-- npcs which belong to their respective accounts
DROP TABLE IF EXISTS `AccountNPCs`;
CREATE TABLE IF NOT EXISTS `AccountNPCs` (
    `AccountInstID` INTEGER  NOT NULL,
    `NPCInstID` INTEGER  NOT NULL,
    FOREIGN KEY (`AccountInstID`) REFERENCES `Account_inst`(`AccountInstID`)
    FOREIGN KEY (`NPCInstID`) REFERENCES `NPC_inst`(`NPCInstID`)
);

-- general definition information for Mob-instantiation (static or usable)
DROP TABLE IF EXISTS `MobDef`;
CREATE TABLE IF NOT EXISTS `MobDef` (
    `MobDefID` INTEGER NOT NULL,
    `MobInterType` INTEGER  NOT NULL DEFAULT 0, -- defaults to 0=None (not an interactive vob/mob)
    `Visual` TEXT NOT NULL,
    `CDDyn` INTEGER  NOT NULL DEFAULT 0, -- defaults to 0=no collision detection with vobs
    `CDStatic` INTEGER  NOT NULL DEFAULT 0, -- defaults to 0=no collision detection with worldmesh
    `FocusName` TEXT DEFAULT NULL, -- name which appears when having mob in focus
    `UseWithItem` INTEGER DEFAULT NULL, -- mob-interaction only possible with this kind of item in inventory
    `TriggerTarget` TEXT DEFAULT NULL, -- ???
    `IsLocked` INTEGER DEFAULT 0, -- whether it is a lockable object or not
    `KeyInstance` INTEGER DEFAULT 0, -- possible kind of key-item to unlock the lockable mob
    `PicklockString` TEXT DEFAULT NULL, -- right-left-pattern for lockpicking
    `HasEffects` INTEGER NOT NULL DEFAULT 0,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`MobDefID`),
    FOREIGN KEY (`UseWithItem`) REFERENCES `Item_def`(`ItemDefID`),
    FOREIGN KEY (`KeyInstance`) REFERENCES `Item_def`(`ItemDefID`)
);

-- general definition information for Spell-instantiation
--- !!! incomplete and is postponed until Spells are actually needed !!!
DROP TABLE IF EXISTS `SpellDef`;
CREATE TABLE IF NOT EXISTS `SpellDef` (
    `SpellDefID` INTEGER  NOT NULL,
    `Name` TEXT NOT NULL,
    `FXName` TEXT NOT NULL,
    `DamageType` INTEGER NOT NULL DEFAULT 0,
    `SpellType` INTEGER NOT NULL DEFAULT 0,
    `IsMultiEffect` INTEGER NOT NULL DEFAULT 0,
    `TargetCollectionAlgo` INTEGER NOT NULL DEFAULT 0,
    `TargetCollectType` INTEGER NOT NULL DEFAULT 0,
    `TargetCollectRange` INTEGER NOT NULL DEFAULT 0,
    `TargetCollectAzi` INTEGER NOT NULL DEFAULT 0,
    `TargetCollectElev` INTEGER NOT NULL DEFAULT 0,
    `HasEffects` INTEGER NOT NULL DEFAULT 0,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`SpellDefID`)
);

-- general definition information for Item-instantiation
DROP TABLE IF EXISTS `ItemDef`;
CREATE TABLE IF NOT EXISTS `ItemDef` (
    `ItemDefID` INTEGER  NOT NULL,
    `InstanceName` TEXT NOT NULL, -- insertcode of that item
    `Name` TEXT NOT NULL, -- displayed name
    `ScemeName` TEXT DEFAULT "", -- ??? (e.g. MAP, MAPSEALED, etc.)
    `Protections` TEXT DEFAULT NULL, -- should be of pattern 0=0,1=0,2=25
    `Damages` TEXT DEFAULT NULL, -- should be of pattern 0=0,1=0,2=25
    `Value` INTEGER NOT NULL DEFAULT 0, -- price of that item (not used in our custom trade?)
    `MainFlag` INTEGER NOT NULL DEFAULT 1, -- what item category (defaults to ITEM_KAT_NONE)
    `Flag` INTEGER NOT NULL, -- defines type of item in Gothic 2 (must be set individually)
    `ArmorFlag` INTEGER DEFAULT NULL, -- ArmorFlags-entry (1, 2, 16 <-> WEAR_TORSO, WEAR_HEAD , WEAR_EFFECT)
    `DamageType` INTEGER DEFAULT NULL, -- type of damage to inflict (only weapons)
    `TotalDamage` INTEGER DEFAULT NULL, -- sum of Damages (why not sum it up internally Gothic 2?)
    `Range` INTEGER DEFAULT NULL, -- weapon range
    `Visual` TEXT NOT NULL, -- name of 3D-model file (.3DS-file)
    `VisualChange` TEXT DEFAULT "", -- for armors (.asc-file)
    `Effect` TEXT DEFAULT "", -- triggered particle effect (e.g. when drinking potions)
    `VisualSkin` INTEGER DEFAULT NULL, -- ???
    `Material` INTEGER NOT NULL DEFAULT 3, -- type of material which determines the collision sound (defaults to MAT_LEATHER)
    `Munition` INTEGER  DEFAULT NULL, -- ID of the munition-Item_def of this table
    `IsKeyInstance` INTEGER NOT NULL DEFAULT 0, -- is it a key?
    `IsTorch` INTEGER NOT NULL DEFAULT 0, -- is it an unused torch?
    `IsTorchBurning` INTEGER NOT NULL DEFAULT 0, -- is it an already burning torch?
    `IsTorchBurned` INTEGER NOT NULL DEFAULT 0, -- is it a finally burned up torch?
    `IsGold` INTEGER NOT NULL DEFAULT 0, -- is it gold/currency? (will be summed up in inventory in seperate display field)
    `HasEffects` INTEGER NOT NULL DEFAULT 0, -- has it additional event-driven effects (e.g. giving hp on drinking the potion)
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`ItemDefID`)
);

-- general definition information for NPC-instantiation
DROP TABLE IF EXISTS `NPCDef`;
CREATE TABLE IF NOT EXISTS `NPCDef` (
    `NPCDefID` INTEGER  NOT NULL,
    `Name` TEXT NOT NULL DEFAULT "Pumper Nickel :)",
    `Attributes` TEXT NOT NULL DEFAULT "0=1,1=1,2=0,3=0,4=0,5=0,6=0,7=0,8=0", -- HP, HPMax, MP, MPMax, STR, DEX, RegenHP, RegenMP, Max???
    `TalentValues` TEXT NOT NULL DEFAULT "0=0,1=0,2=0,3=0,4=0,5=0,6=0,7=0,8=0,9=0,10=0,11=0,12=0,13=0,14=0,15=0,16=0,17=0,18=0,19=0", -- NOT USED BY GUC AT THE MOMENT! Unknown, H1, H2, Bow, Crossbow, Picklock, Pickpocket, Mage, Sneak, Regenerate, Firemaster, Acrobat, PickpocketG2, Smith, Runes, Alchemy, TakeAnimalTrophy, Foreignlanguage, MaxTalents
    `TalentSkills` TEXT NOT NULL DEFAULT "0=0,1=0,2=0,3=0,4=0,5=0,6=0,7=0,8=0,9=0,10=0,11=0,12=0,13=0,14=0,15=0,16=0,17=0,18=0,19=0", -- NOT USED BY GUC AT THE MOMENT! Unknown, H1, H2, Bow, Crossbow, Picklock, Pickpocket, Mage, Sneak, Regenerate, Firemaster, Acrobat, PickpocketG2, Smith, Runes, Alchemy, TakeAnimalTrophy, Foreignlanguage, MaxTalents
    `HitChances` TEXT NOT NULL DEFAULT "0=1,1=1,2=0,3=0", -- H1, H2, Bow, Crossbow
    `Guild` INTEGER  DEFAULT NULL,
    `Voice` INTEGER  NOT NULL DEFAULT 0,
    `Visual` TEXT NOT NULL,
    `BodyMesh` TEXT NOT NULL,
    `BodyTex` INTEGER  NOT NULL,
    `SkinColor` INTEGER  NOT NULL,
    `HeadMesh` TEXT NOT NULL,
    `HeadTex` INTEGER  NOT NULL,
    `TeethTex` INTEGER  NOT NULL,
    `HasEffects`
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`NPCDefID`)
);

-- general definition information for effect-definitions
DROP TABLE IF EXISTS `DefEffect`;
CREATE TABLE IF NOT EXISTS `DefEffect` (
    `EffectDefID` INTEGER  NOT NULL,
    `Name` TEXT NOT NULL DEFAULT "",
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`EffectDefID`)
);

-- general definition information for change definition that actually apply to the game
DROP TABLE IF EXISTS `DefChange`;
CREATE TABLE IF NOT EXISTS `DefChange` (
    `DefChangeID` INTEGER NOT NULL,
    `DefEffectID` INTEGER  NOT NULL,
    `ChangeType` INTEGER  NOT NULL,
    `Parameters` TEXT NOT NULL DEFAULT "",
    PRIMARY KEY (`DefChangeID`),
    FOREIGN KEY (`DefEffectID`) REFERENCES `DefEffect`(`DefEffectID`)
);

-- maps effect- to mob-definitions
DROP TABLE IF EXISTS `MobDefEffect`;
CREATE TABLE IF NOT EXISTS `MobDefEffect` (
    `MobDefID` INTEGER  NOT NULL,
    `DefEffectID` INTEGER  NOT NULL,
    FOREIGN KEY (`MobDefID`) REFERENCES `MobDef`(`MobDefID`),
    FOREIGN KEY (`DefEffectID`) REFERENCES `DefEffect`(`DefEffectID`)
);

-- maps effect- to spell-definitions
DROP TABLE IF EXISTS `SpellDefEffect`;
CREATE TABLE IF NOT EXISTS `SpellDefEffect` (
    `SpellDefID` INTEGER  NOT NULL,
    `DefEffectID` INTEGER  NOT NULL,
    FOREIGN KEY (`SpellDefID`) REFERENCES `SpellDef`(`SpellDefID`),
    FOREIGN KEY (`DefEffectID`) REFERENCES `DefEffect`(`DefEffectID`)
);

-- maps effect- to item-definitions
DROP TABLE IF EXISTS `ItemDefEffect`;
CREATE TABLE IF NOT EXISTS `ItemDefEffect` (
    `ItemDefID` INTEGER  NOT NULL,
    `DefEffectID` INTEGER  NOT NULL,
    FOREIGN KEY (`ItemDefID`) REFERENCES `ItemDef`(`ItemDefID`),
    FOREIGN KEY (`DefEffectID`) REFERENCES `DefEffect`(`DefEffectID`)
);

-- maps effect- to npc-definitions
DROP TABLE IF EXISTS `NPCDefEffect`;
CREATE TABLE IF NOT EXISTS `NPCDefEffect` (
    `NPCDefID` INTEGER  NOT NULL,
    `DefEffectID` INTEGER  NOT NULL,
    FOREIGN KEY (`NPCDefID`) REFERENCES `NPCDef`(`NPCDefID`),
    FOREIGN KEY (`DefEffectID`) REFERENCES `DefEffect`(`DefEffectID`)
);

-- individual mob instances
DROP TABLE IF EXISTS `MobInst`;
CREATE TABLE IF NOT EXISTS `MobInst` (
    `MobInstID` INTEGER  NOT NULL,
    `IsSpawned` INTEGER NOT NULL,
    `MobDefID` INTEGER  NOT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`MobInstID`)
);

-- individual item instances
DROP TABLE IF EXISTS `ItemInst`;
CREATE TABLE IF NOT EXISTS `ItemInst` (
    `ItemInstID` INTEGER NOT NULL,
    `IsSpawned` INTEGER NOT NULL,
    `ItemDefID` INTEGER  NOT NULL,
    `Amount` INTEGER  NOT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`ItemInstID`)
);

-- individual npc instances
DROP TABLE IF EXISTS `NPCInst`;
CREATE TABLE IF NOT EXISTS `NPCInst` (
    `NPCInstID` INTEGER  NOT NULL,
    `IsSpawned` INTEGER NOT NULL,
    `NPCDefID` INTEGER NOT NULL,
    `Fatness` INTEGER  NOT NULL DEFAULT 1,
    `Scale` TEXT NOT NULL DEFAULT "0=1,1=1,2=1", -- scale in x-, y- and z-directions respectively
    `Guild` INTEGER NOT NULL DEFAULT 0,
    `Voice` INTEGER NOT NULL DEFAULT 0,
    `Visual` TEXT DEFAULT NULL,
    `BodyMesh` TEXT NOT NULL,
    `BodyTexture` INTEGER NOT NULL,
    `HeadMesh` TEXT NOT NULL,
    `HeadTexture` INTEGER NOT NULL,
    `CurrWalk` INTEGER  NOT NULL,
    `CurrAnimation` TEXT DEFAULT "",
    `HP` INTEGER NOT NULL DEFAULT 1,
    `HPMax` INTEGER NOT NULL DEFAULT 1,
    `MP` INTEGER NOT NULL DEFAULT 0,
    `MPMax` INTEGER NOT NULL DEFAULT 0,
    `Strength` INTEGER NOT NULL DEFAULT 1,
    `Dexterity` INTEGER NOT NULL DEFAULT 1,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    PRIMARY KEY (`NPCInstID`)
);

-- individual item instances which are positioned in npc inventories
DROP TABLE IF EXISTS `InventoryItemInst`;
CREATE TABLE IF NOT EXISTS `InventoryItemInst` (
    `NPCInstID` INTEGER  NOT NULL,
    `ItemInstID` INTEGER  NOT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    FOREIGN KEY (`ItemInstID`) REFERENCES `ItemInst`(`ItemInstID`),
    FOREIGN KEY (`NPCInstID`) REFERENCES `NPCInst`(`NPCInstID`)
);

-- individual item instances which are positioned in containers/mobs
DROP TABLE IF EXISTS `ContainerItemInst`;
CREATE TABLE IF NOT EXISTS `ContainerItemInst` (
    `ItemInstID` INTEGER  NOT NULL,
    `MobInstID` INTEGER  NOT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    FOREIGN KEY (`ItemInstID`) REFERENCES `ItemInst`(`ItemInstID`),
    FOREIGN KEY (`MobInstID`) REFERENCES `MobInst`(`ItemInstID`)
);

-- individual mob instances which are positioned in one of the worlds
DROP TABLE IF EXISTS `WorldMobInst`;
CREATE TABLE IF NOT EXISTS `WorldMobInst` (
    `MobInstID` INTEGER  NOT NULL,
    `WorldInstID` INTEGER  NOT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    FOREIGN KEY (`MobInstID`) REFERENCES `MobInst`(`MobInstID`),
    FOREIGN KEY (`WorldInstID`) REFERENCES `WorldInst`(`WorldInstID`)
);

-- individual item instances which are positioned in one of the worlds
DROP TABLE IF EXISTS `WorldItemInst`;
CREATE TABLE IF NOT EXISTS `WorldItemInst` (
    `ItemInstID` INTEGER  NOT NULL,
    `WorldInstID` INTEGER  NOT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    FOREIGN KEY (`ItemInstID`) REFERENCES `ItemInst`(`ItemInstID`),
    FOREIGN KEY (`WorldInstID`) REFERENCES `WorldInst`(`WorldInstID`)
);

-- individual npc instances which are positioned in one of the worlds
DROP TABLE IF EXISTS `WorldNPCInst`;
CREATE TABLE IF NOT EXISTS `WorldNPCInst` (
    `NPCInstID` INTEGER  NOT NULL,
    `WorldInstID` INTEGER  NOT NULL,
    `ChangeDate` TEXT NOT NULL,
    `CreationDate` TEXT NOT NULL,
    FOREIGN KEY (`NPCInstID`) REFERENCES `NPCInst`(`NPCInstID`),
    FOREIGN KEY (`WorldInstID`) REFERENCES `WorldInst`(`WorldInstID`)
);

/* 
------------------------
-- Test-MobDef(s)
------------------------

-- a bed in which the exhausted find no sleep
INSERT OR REPLACE INTO `Mob_def` (ID, MobInterType, Visual, CDDyn, CDStatic, HasEffects, ChangeDate, CreationDate) VALUES (0, 0, "DT_Bed_V1.3ds", 1, 1, 0, "2015-05-11", "2015-05-11");

------------------------
-- Test-ItemDef(s)
------------------------

-- ITMI_MOREMONEY
INSERT OR REPLACE INTO `Item_def` (ID, InstanceName, Name, ScemeName, MainFlag, Flag, Visual, HasEffects, ChangeDate, CreationDate) VALUES (0, "ITMI_MOREMONEY", "MoreMoney", "", 1, 2097152, "ItMi_Gold.3ds", 1, "2015-05-11", "2015-05-11");
INSERT OR REPLACE INTO `Effect_def` (ID, Name, ChangeDate, CreationDate) VALUES (0, "Item_ITMI_MOREMONEY", "2015-05-11", "2015-05-11");
INSERT OR REPLACE INTO `Effect_Changes_def` (ID, EffectDefID, ChangeType, Parameters) VALUES (0, 0, 1000, "More money better than less money ya know..."), (1, 0, 1001, "Awesomeness"), (2, 0, 1007, 9001);
INSERT OR REPLACE INTO `Item_Effects_inst` (ItemDefID, EffectDefID) VALUES (0, 0);

------------------------
-- Test-NPCDef(s)
------------------------
 */
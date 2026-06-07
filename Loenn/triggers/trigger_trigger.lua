local enums = require "consts.celeste_enums"

local activationTypes = {
    ["Flag (Default)"] = "Flag",
    ["Dashing"] = "Dashing",
    ["Dash Count"] = "DashCount",
    ["Deaths In Room"] = "DeathsInRoom",
    ["Deaths In Level"] = "DeathsInLevel",
    ["Holdable Grabbed"] = "GrabHoldable",
    ["Horizontal Speed"] = "SpeedX",
    ["Vertical Speed"] = "SpeedY",
    ["Jumping"] = "Jumping",
    ["Crouching"] = "Crouching",
    ["Time Since Player Moved"] = "TimeSinceMovement",
    ["Holdable Entered"] = "OnHoldableEnter",
    ["On Entity Touch"] = "OnEntityCollide",
    ["Core Mode"] = "CoreMode",
    ["On Interaction"] = "OnInteraction",
    ["Touched Solid"] = "OnSolid",
    ["Entity Entered"] = "OnEntityEnter",
    ["On Input"] = "OnInput",
    ["Grounded"] = "OnGrounded",
    ["Player State"] = "OnPlayerState",
    ["Counter"] = "Counter",
    ["Slider"] = "Slider",
}

local comparisonTypes = {
    "LessThan",
    "EqualTo",
    "GreaterThan"
}

local inputTypes = {
    "Left",
    "Right",
    "Up",
    "Down",
    "Jump",
    "Dash",
    "Grab",
    "Interact",
    "CrouchDash",
    "Any",
}

local triggerTrigger = {}

triggerTrigger.name = "vitellary/triggertrigger"

triggerTrigger.nodeLimits = {1, -1}
triggerTrigger.nodeLineRenderType = "fan"

triggerTrigger.fieldInformation = {
    activationType = {
        editable = false,
        options = activationTypes
    },
    comparisonType = {
        editable = false,
        options = comparisonTypes
    },
    inputType = {
        editable = false,
        options = inputTypes
    },
    coreMode = {
        editable = false,
        options = enums.core_modes
    },
    playerState = {
        fieldType = "integer",
    }
}

function triggerTrigger.ignoredFields(entity)
    local ignored = {
        "_id",
        "_name",
        "comparisonType",
        "absoluteValue",
        "talkBubbleX",
        "talkBubbleY",
        "flag",
        "deaths",
        "dashCount",
        "requiredSpeed",
        "timeToWait",
        "coreMode",
        "entityTypeToCollide",
        "collideCount",
        "solidType",
        "entityType",
        "inputType",
        "holdInput",
        "excludeTalkers",
        "onlyIfSafe",
        "playerState",
        "includeCoyote",
        "includeWallJump",
        "resetAfterJump",
        "counterValue",
        "sliderValue",
        "counterName",
        "sliderName",
    }

    local function doNotIgnore(value)
        for i = #ignored, 1, -1 do
            if ignored[i] == value then
                table.remove(ignored, i)
                return
            end
        end
    end

    local atype = entity.activationType or "Flag"
    local iscomparison = false

    if atype == "Flag" then
        doNotIgnore("flag")
    elseif atype == "DashCount" then
        doNotIgnore("dashCount")
        iscomparison = true
    elseif atype == "DeathsInRoom" or atype == "DeathsInLevel" then
        doNotIgnore("deaths")
        iscomparison = true
    elseif atype == "SpeedX" or atype == "SpeedY" then
        doNotIgnore("requiredSpeed")
        iscomparison = true
    elseif atype == "TimeSinceMovement" then
        doNotIgnore("timeToWait")
        iscomparison = true
    elseif atype == "CoreMode" then
        doNotIgnore("coreMode")
    elseif atype == "OnEntityCollide" then
        doNotIgnore("entityType")
        doNotIgnore("collideCount")
        iscomparison = true
    elseif atype == "OnInteraction" then
        doNotIgnore("talkBubbleX")
        doNotIgnore("talkBubbleY")
    elseif atype == "OnSolid" then
        doNotIgnore("solidType")
    elseif atype == "OnEntityEnter" then
        doNotIgnore("entityType")
    elseif atype == "OnInput" then
        doNotIgnore("inputType")
        doNotIgnore("holdInput")
        if entity.inputType == "Interact" then
            doNotIgnore("excludeTalkers")
        end
    elseif atype == "OnGrounded" then
        doNotIgnore("onlyIfSafe")
        doNotIgnore("includeCoyote")
    elseif atype == "Jumping" then
        doNotIgnore("includeWallJump")
        doNotIgnore("resetAfterJump")
    elseif atype == "OnPlayerState" then
        doNotIgnore("playerState")
    elseif atype == "Counter" then
        doNotIgnore("counterValue")
        doNotIgnore("counterName")
        iscomparison = true
    elseif atype == "Slider" then
        doNotIgnore("sliderName")
        doNotIgnore("sliderValue")
        iscomparison = true
    end

    if iscomparison then
        doNotIgnore("comparisonType")
        doNotIgnore("absoluteValue")
    end

    return ignored
end

triggerTrigger.placements = {}
for _, mode in pairs(activationTypes) do
    local placement = {
        name = string.lower(mode),
        data = {
            oneUse = false,
            flag = "",
            invertCondition = false,
            delay = 0.0,
            activateOnTransition = false,
            randomize = false,
            matchPosition = true,
            activationType = mode,
            comparisonType = "EqualTo",
            deaths = 0,
            dashCount = 0,
            requiredSpeed = 0.0,
            absoluteValue = false,
            timeToWait = 0.0,
            coreMode = "None",
            entityTypeToCollide = "Celeste.Strawberry",
            talkBubbleX = 0,
            talkBubbleY = 0,
            onlyOnEnter = false,
            collideCount = 1,
            solidType = "",
            entityType = "",
            inputType = "Grab",
            holdInput = false,
            excludeTalkers = false,
            onlyIfSafe = false,
            playerState = 0,
            includeCoyote = false,
            includeWallJump = true,
            resetAfterJump = false,
            counterValue = "0",
            sliderValue = "0.0",
            counterName = "counter",
            sliderName = "slider",
        }
    }
    table.insert(triggerTrigger.placements, placement)
end

return triggerTrigger

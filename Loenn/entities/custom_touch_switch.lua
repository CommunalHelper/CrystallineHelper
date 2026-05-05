local drawableSprite = require "structs.drawable_sprite"
local atlases = require("atlases")

local customTouchSwitch = {}

customTouchSwitch.name = "vitellary/customtouchswitch"
customTouchSwitch.placements = {
    {
        name = "custom_touch_switch",
        data = {
            flag = "",
            inactiveColor = "5FCDE4",
            movingColor = "FF7F7F",
            activeColor = "FFFFFF",
            finishColor = "F141DF",
            moveTime = 1.25,
            easing = "CubeOut",
            icon = "vanilla",
            inverted = false,
            persistent = false,
            smoke = true,
            allowDisable = true,
            badelineDeactivate = false,
            randomOrder = false,
            pathLength = -1,
            revisitPreviousNodes = false,
            border = "vanilla"
        }
    }
}

customTouchSwitch.fieldOrder = {"x","y","moveTime","flag","icon","border","inactiveColor","activeColor","finishColor","movingColor","easing","pathLength"}

local easeTypes = {
    "Linear", "SineIn", "SineOut", "SineInOut", "QuadIn", "QuadOut", "QuadInOut", "CubeIn", "CubeOut", "CubeInOut", "QuintIn", "QuintOut", "QuintInOut", "BackIn", "BackOut", "BackInOut", "ExpoIn", "ExpoOut", "ExpoInOut", "BigBackIn", "BigBackOut", "BigBackInOut", "ElasticIn", "ElasticOut", "ElasticInOut", "BounceIn", "BounceOut", "BounceInOut"
}
customTouchSwitch.fieldInformation = {
    easing = {
        editable = false,
        options = easeTypes,
    },
    icon = {
        options = {"vanilla", "tall", "triangle", "circle"},
    },
    pathLength = {
        fieldType = "integer",
    },
    border = {
        default = "vanilla"
    },
}

function customTouchSwitch.ignoredFields(ent)
    if ent.randomOrder then
        return {"_name", "_id", "originX", "originY"}
    else
        return {"_name", "_id", "originX", "originY", "pathLength", "revisitPreviousNodes"}
    end
end

customTouchSwitch.nodeLimits = {0, -1}
customTouchSwitch.nodeLineRenderType = "line"

function customTouchSwitch.sprite(room, entity)
    local borderResource = "objects/touchswitch/container"
    if (entity.border ~= nil) and (entity.border ~= "vanilla") and atlases.gameplay["objects/customMovingTouchSwitch/" .. entity.border] then
        borderResource = "objects/customMovingTouchSwitch/" .. entity.border
    end
    local containerSprite = drawableSprite.fromTexture(borderResource, entity)

    local iconResource = "objects/touchswitch/icon00"
    if entity.icon ~= "vanilla" then
        iconResource = "objects/" .. (entity.icon == "tall" or entity.icon == "triangle" or entity.icon == "circle" ? "CrystallineHelper/FLCC/" : "") .. "customMovingTouchSwitch/" .. entity.icon .."/icon00"
    end

    local iconSprite = drawableSprite.fromTexture(iconResource, entity)

    return {containerSprite, iconSprite}
end

return customTouchSwitch
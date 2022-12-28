local drawableSprite = require "structs.drawable_sprite"

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
        }
    }
}

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
}

customTouchSwitch.nodeLimits = {0, -1}
customTouchSwitch.nodeLineRenderType = "line"

function customTouchSwitch.sprite(room, entity)
    local containerSprite = drawableSprite.fromTexture("objects/touchswitch/container", entity)

    local iconResource = "objects/touchswitch/icon00"
    if entity.icon ~= "vanilla" then
        iconResource = "objects/customMovingTouchSwitch/" .. entity.icon .."/icon00"
    end

    local iconSprite = drawableSprite.fromTexture(iconResource, entity)

    return {containerSprite, iconSprite}
end

return customTouchSwitch
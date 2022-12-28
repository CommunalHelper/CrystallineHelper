local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableRectangle = require("structs.drawable_rectangle")
local drawableSprite = require("structs.drawable_sprite")

local bumperBlock = {}

local axesOptions = {
    Both = "both",
    Vertical = "vertical",
    Horizontal = "horizontal"
}

bumperBlock.name = "vitellary/bumperblock"
bumperBlock.depth = 0
bumperBlock.minimumSize = {24, 24}
bumperBlock.fieldInformation = {
    axes = {
        options = axesOptions,
        editable = false
    }
}
bumperBlock.placements = {}

for _, axis in pairs(axesOptions) do
    table.insert(bumperBlock.placements, {
        name = axis,
        data = {
            width = 24,
            height = 24,
            axes = axis,
        }
    })
end

local frameTextures = {
    none = "objects/bumperBlock/block00",
    horizontal = "objects/bumperBlock/block01",
    vertical = "objects/bumperBlock/block02",
    both = "objects/bumperBlock/block03"
}

local ninePatchOptions = {
    mode = "border",
    borderMode = "repeat"
}

local kevinColor = {98 / 255, 34 / 255, 43 / 255}
local faceTexture = "objects/bumperBlock/idle_face"

function bumperBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local axes = entity.axes or "both"
    local chillout = entity.chillout

    local frameTexture = frameTextures[axes] or frameTextures["both"]
    local ninePatch = drawableNinePatch.fromTexture(frameTexture, ninePatchOptions, x, y, width, height)

    local rectangle = drawableRectangle.fromRectangle("fill", x + 2, y + 2, width - 4, height - 4, kevinColor)
    local faceSprite = drawableSprite.fromTexture(faceTexture, entity)

    faceSprite:addPosition(math.floor(width / 2), math.floor(height / 2))

    local sprites = ninePatch:getDrawableSprite()

    table.insert(sprites, 1, rectangle:getDrawableSprite())
    table.insert(sprites, 2, faceSprite)

    return sprites
end

return bumperBlock
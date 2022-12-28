local utils = require "utils"
local drawableSprite = require "structs.drawable_sprite"

local flagCrystal = {}

flagCrystal.name = "vitellary/flagcrystal"
flagCrystal.placements = {
    {
        name = "flag_crystal",
        data = {
            flag = "",
            spawnFlag = "",
            color = "ffffff",
            sprite = "flagCrystal",
            theo = false,
        }
    }
}

flagCrystal.justification = {0.5, 1}
function flagCrystal.sprite(room, entity)
    local path = "objects/"..entity.sprite.."/"
    local tint = utils.getColor(entity.color)
    local sprites = {}

    local back = drawableSprite.fromTexture(path.."back", entity)
    back:setColor(tint)
    table.insert(sprites, back)

    if entity.theo then
        local theo = drawableSprite.fromTexture(path.."theo", entity)
        theo:setColor(tint)
        table.insert(sprites, theo)
    end

    local front = drawableSprite.fromTexture(path.."front", entity)
    front:setColor(tint)
    table.insert(sprites, front)

    if entity.sprite == "flagCrystal" then
        -- offset it to look like how it'd look ingame if default
        for _,sprite in ipairs(sprites) do
            sprite:addPosition(-1, -3)
        end
    end

    return sprites
end

return flagCrystal
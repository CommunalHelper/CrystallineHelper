module FlushelineCustomHeart

using ..Ahorn, Maple

@mapdef Entity "vitellary/customheart" CustomHeart(x::Integer, y::Integer, slowdown::Bool=false, endLevel::Bool=false, oneUse::Bool=false, respawnTime::Number=3.0, poemId::String="", type::String="Blue", path::String="heartGemColorable", color::String="ff4fed", bloom::Number=0.75, light::Bool=true, bully::Bool=false, additionalEffects::Bool=true, switchCoreMode::Bool=false, colorGrade::Bool=false, static::Bool=false, dashCount::Integer=1)

const placements = Ahorn.PlacementDict(
    "Custom Fake Heart (Crystalline)" => Ahorn.EntityPlacement(
        CustomHeart
    )
)

spriteTypes = String["Blue", "Red", "Gold", "Custom", "Core", "CoreInverted", "Random"]

Ahorn.editingOptions(entity::CustomHeart) = Dict{String, Any}(
    "type" => spriteTypes
)
Ahorn.editingOrder(entity::CustomHeart) = String["x", "y", "respawnTime", "dashCount", "poemId", "color", "path", "type", "bloom", "endLevel", "slowdown", "light", "additionalEffects", "oneUse", "bully", "switchCoreMode", "colorGrade"]

function getSprites(entity::CustomHeart)
    type = get(entity.data, "type", "Blue")

    if type == "Blue"
        return String["collectables/heartGem/0/00.png"]
    elseif type == "Red"
        return String["collectables/heartGem/1/00.png"]
    elseif type == "Gold"
        return String["collectables/heartGem/2/00.png"]
    elseif type == "Core"
        return String["ahorn_customcoreheart.png"]
    elseif type == "CoreInverted"
        return String["ahorn_customcoreheartinverted.png"]
    elseif type == "Custom"
        path = get(entity.data, "path", "")
        return String["collectables/"*path*"/00.png", "collectables/"*path*"/outline00.png"]
    end

    return String["collectables/heartGem/0/00.png"]
end

function Ahorn.selection(entity::CustomHeart)
    x, y = Ahorn.position(entity)
    sprite = getSprites(entity)[1]

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomHeart, room::Maple.Room)
    sprites = getSprites(entity)
    type = get(entity.data, "type", "Blue")

    for i in 1:length(sprites)
        color = nothing
        if i == 1 && type == "Custom"
            rawColor = Ahorn.argb32ToRGBATuple(parse(Int, get(entity.data, "color", "ffffff"), base=16))[1:3] ./ 255
            color = (rawColor..., 1.0)
        end
        sprite = Ahorn.getTextureSprite(sprites[i])
        if i == 1 || sprite != Ahorn.fileNotFoundSpriteHolder.sprite
            Ahorn.drawSprite(ctx, sprites[i], 0, 0, tint=color)
        end
    end
end

end
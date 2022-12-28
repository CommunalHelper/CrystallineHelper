module FlushelineFlagCrystal

using ..Ahorn, Maple

@mapdef Entity "vitellary/flagcrystal" Crystal(x::Integer, y::Integer, flag::String="", spawnFlag::String="", color::String="ffffff", sprite::String="flagCrystal", theo::Bool=false, invertFlag::Bool=false)

const placements = Ahorn.PlacementDict(
    "Flag Crystal (Crystalline)" => Ahorn.EntityPlacement(
        Crystal
    )
)

function Ahorn.selection(entity::Crystal)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 10, y - 22, 21, 22)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Crystal, room::Maple.Room)
	color = ((Ahorn.argb32ToRGBATuple(parse(Int, get(entity.data, "color", "ffffff"), base=16))[1:3] ./ 255)..., 1.0)
    sprite = "objects/" * get(entity.data, "sprite", "flagCrystal")
	Ahorn.drawImage(ctx, hasSprite("$sprite/back") ? "$sprite/back" : "objects/flagCrystal/back", -10, -22, tint=color)
	if get(entity.data, "theo", false)
		Ahorn.drawImage(ctx, hasSprite("$sprite/theo") ? "$sprite/theo" : "objects/flagCrystal/theo", -10, -22)
	end
	Ahorn.drawImage(ctx, hasSprite("$sprite/front") ? "$sprite/front" : "objects/flagCrystal/front", -10, -22, tint=color)
end

hasSprite(sprite::String) = Ahorn.getSprite(sprite) != Ahorn.fileNotFoundSpriteHolder.sprite

end
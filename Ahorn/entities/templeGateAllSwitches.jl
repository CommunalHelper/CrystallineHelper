module FlushelineTempleGateAllSwitches

using ..Ahorn, Maple

@mapdef Entity "vitellary/templegateall" Gate(x::Integer, y::Integer, sprite::String="default")

const textures = String["default", "mirror", "theo"]

const placements = Ahorn.PlacementDict(
    "Temple Gate (Default, All Switches) (Crystalline)" => Ahorn.EntityPlacement(
        Gate,
		"point",
		Dict{String, Any}(
            "sprite" => "default"
        )
    )
)

Ahorn.editingOptions(entity::Gate) = Dict{String, Any}(
    "sprite" => textures
)

function Ahorn.selection(entity::Gate)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 4, y, 15, 48)
end

const sprites = Dict{String, String}(
    "default" => "objects/door/TempleDoor00",
    "mirror" => "objects/door/TempleDoorB00",
    "theo" => "objects/door/TempleDoorC00"
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Gate, room::Maple.Room)
    sprite = get(entity.data, "sprite", "default")

    if haskey(sprites, sprite)
        Ahorn.drawImage(ctx, sprites[sprite], -4, 0)
    else
		Ahorn.drawImage(ctx, "objects/door/TempleDoor00", -4, 0)
	end
end

end
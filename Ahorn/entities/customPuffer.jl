module FlushelineCustomPuffer

using ..Ahorn, Maple

@mapdef Entity "vitellary/custompuffer" CustomPuffer(x::Integer, y::Integer, right::Bool=false, static::Bool=false, alwaysShowOutline::Bool=true, pushAnyDir::Bool=false, oneUse::Bool=false, angle::Number=0.0, radius::Integer=32, launchSpeed::Number=280.0, respawnTime::Number=2.5, sprite::String="pufferFish", deathFlag::String="", holdable::Bool=false, outlineColor::String="FFFFFF", returnToStart::Bool=true, holdFlip::Bool=false, boostMode::String="SetSpeed")

const placements = Ahorn.PlacementDict(
    "Custom Puffer (Right) (Crystalline)" => Ahorn.EntityPlacement(
        CustomPuffer,
        "point",
        Dict{String, Any}(
            "right" => true
        )
    ),
    "Custom Puffer (Left) (Crystalline)" => Ahorn.EntityPlacement(
        CustomPuffer,
        "point",
        Dict{String, Any}(
            "right" => false
        )
    )
)

const boostmodes = Dict{String, String}(
    "Set Speed" => "SetSpeed",
    "Redirect Speed" => "RedirectSpeed",
    "Redirect + Add Speed" => "AddRedirectSpeed"
)

sprite = "objects/puffer/idle00"

Ahorn.editingOptions(entity::CustomPuffer) = Dict{String, Any}(
    "boostMode" => boostmodes
)

function Ahorn.selection(entity::CustomPuffer)
    x, y = Ahorn.position(entity)
    scaleX = get(entity, "right", false) ? 1 : -1

    return Ahorn.getSpriteRectangle(sprite, x, y, sx=scaleX)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomPuffer, room::Maple.Room)
    scaleX = get(entity, "right", false) ? 1 : -1

    Ahorn.drawSprite(ctx, sprite, 0, 0, sx=scaleX)
end

function Ahorn.flipped(entity::CustomPuffer, horizontal::Bool)
    if horizontal
        entity.right = !entity.right

        return entity
    end
end

end
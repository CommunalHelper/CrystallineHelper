module FlushelineEnergyBooster

using ..Ahorn, Maple

@mapdef Entity "vitellary/energybooster" EnergyBooster(x::Integer, y::Integer, behaveLikeDash::Bool=false, redirectSpeed::Bool=false, oneUse::Bool=false)

const placements = Ahorn.PlacementDict(
    "Energy Booster (Crystalline)" => Ahorn.EntityPlacement(
        EnergyBooster
    ),
    "Energy Booster (Redirect) (Crystalline)" => Ahorn.EntityPlacement(
        EnergyBooster,
        "point",
        Dict{String, Any}(
            "redirectSpeed" => true
        )
    ),
)

sprite = "objects/energyBooster/booster00"
altSprite = "objects/energyBoosterRedirect/booster00"

function Ahorn.selection(entity::EnergyBooster)
    x, y = Ahorn.position(entity)
    redirect = get(entity.data, "redirectSpeed", false)

    return Ahorn.getSpriteRectangle(redirect ? altSprite : sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::EnergyBooster, room::Maple.Room)
    redirect = get(entity.data, "redirectSpeed", false)

    Ahorn.drawSprite(ctx, redirect ? altSprite : sprite, 0, 0)
end

end
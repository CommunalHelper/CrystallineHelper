module CassetteFlagController

using ..Ahorn, Maple

@mapdef Entity "vitellary/cassetteflags" Controller(x::Integer, y::Integer, blueFlag::String="cas_blue", pinkFlag::String="cas_rose", yellowFlag::String="cas_brightsun", greenFlag::String="cas_malachite")

const placements = Ahorn.PlacementDict(
    "Cassette Flag Controller (Crystalline)" => Ahorn.EntityPlacement(
        Controller
    )
)

function Ahorn.selection(entity::Controller)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 12, y - 12, 24, 24)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Controller, room::Maple.Room) = Ahorn.drawSprite(ctx, "ahorn_cassetteflagcontroller.png", 0, 0)

end
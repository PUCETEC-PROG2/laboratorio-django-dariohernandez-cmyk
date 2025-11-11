from django.contrib import admin
from .models import Pokemon, Trainer

@admin.register(Pokemon)
class PokemonAdmin(admin.ModelAdmin):
    pass

@admin.register(Trainer)
class EntrenadorAdmin(admin.ModelAdmin):
    pass


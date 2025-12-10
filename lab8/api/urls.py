
from rest_framework import routers
from django.urls import path, include
from .views import PokemonViewSet, TrainerViewSet

router = routers.DefaultRouter()
router.register(r'pokemons', PokemonViewSet)
router.register(r'trainers', TrainerViewSet)

urlpatterns = [
    path('', include(router.urls)),
]




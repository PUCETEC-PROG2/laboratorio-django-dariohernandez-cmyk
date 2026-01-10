from rest_framework import viewsets
from rest_framework.permissions import AllowAny, IsAuthenticated
from oauth2_provider.contrib.rest_framework import (
    OAuth2Authentication,
    TokenHasReadWriteScope
)

from pokedex.models import Pokemon, Trainer
from .serializers import PokemonSerializer, TrainerSerializer

class PokemonViewSet(viewsets.ModelViewSet):
    queryset = Pokemon.objects.all()
    serializer_class = PokemonSerializer

    # Autenticación OAuth2
    authentication_classes = [OAuth2Authentication]
    required_scopes = ['write']

    def get_permissions(self):
        # Permisos para GET (solo autenticados con token)
        if self.request.method in ['GET', 'HEAD', 'OPTIONS']:
            return [IsAuthenticated(), TokenHasReadWriteScope()]

        # Otros métodos (POST, PUT, DELETE)
        return [AllowAny()]

class TrainerViewSet(viewsets.ModelViewSet):
    queryset = Trainer.objects.all()
    serializer_class = TrainerSerializer

    # Autenticación OAuth2
    authentication_classes = [OAuth2Authentication]
    required_scopes = ['write']

    def get_permissions(self):
        # Permisos para GET (solo autenticados con token)
        if self.request.method in ['GET', 'HEAD', 'OPTIONS']:
            return [IsAuthenticated(), TokenHasReadWriteScope()]

        # Otros métodos (POST, PUT, DELETE)
        return [AllowAny()]

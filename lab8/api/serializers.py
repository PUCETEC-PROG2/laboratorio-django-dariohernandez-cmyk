# api/serializers.py

from rest_framework import serializers
from pokedex.models import Pokemon, Trainer
from django.core.files.base import ContentFile
import base64


class PokemonSerializer(serializers.ModelSerializer):
    picture = serializers.CharField(required=False, allow_null=True)

    class Meta:
        model = Pokemon
        fields = "__all__"

    def validate_picture(self, value):
        if value and isinstance(value, str) and value.startswith("data:image"):
            try:
                format, imgstr = value.split(";base64,")
                ext = format.split("/")[-1]
                return ContentFile(base64.b64decode(imgstr), name=f"pokemon.{ext}")
            except Exception:
                raise serializers.ValidationError("La imagen no es base64 válida.")
        return value


class TrainerSerializer(serializers.ModelSerializer):
    picture = serializers.CharField(required=False, allow_null=True)

    class Meta:
        model = Trainer
        fields = "__all__"

    def validate_picture(self, value):
        if value and isinstance(value, str) and value.startswith("data:image"):
            try:
                format, imgstr = value.split(";base64,")
                ext = format.split("/")[-1]
                return ContentFile(base64.b64decode(imgstr), name=f"trainer.{ext}")
            except Exception:
                raise serializers.ValidationError("La imagen no es base64 válida.")
        return value

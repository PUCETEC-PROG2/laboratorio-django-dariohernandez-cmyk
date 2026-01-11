from django.shortcuts import render
from django.http import HttpResponse, JsonResponse
from django.views.decorators.csrf import csrf_exempt
from django.contrib.auth.models import User
import json

# Vista principal
def index(request):
    return HttpResponse("¡Bienvenido a la Pokedex!")

# Vista de detalle de Pokémon
def pokemon(request, pokemon_id):
    return HttpResponse(f"Detalle del Pokémon con ID {pokemon_id}")

# Vista de detalle de entrenador
def trainer(request, trainer_id):
    return HttpResponse(f"Detalle del entrenador con ID {trainer_id}")

# Agregar un Pokémon
def add_pokemon(request):
    return HttpResponse("Agregar un Pokémon")

# Editar un Pokémon
def edit_pokemon(request, pokemon_id):
    return HttpResponse(f"Editar Pokémon con ID {pokemon_id}")

# Eliminar un Pokémon
def delete_pokemon(request, pokemon_id):
    return HttpResponse(f"Eliminar Pokémon con ID {pokemon_id}")

# API para registro
@csrf_exempt
def api_register(request):
    if request.method == 'POST':
        try:
            data = json.loads(request.body)
            user = User.objects.create_user(
                username=data['username'],
                password=data['password']
            )
            return JsonResponse({'message': 'Usuario creado correctamente'}, status=201)
        except Exception as e:
            return JsonResponse({'error': str(e)}, status=400)

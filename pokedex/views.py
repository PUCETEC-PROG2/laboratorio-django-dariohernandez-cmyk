from django.http import HttpResponse
from django.template import loader
from django.shortcuts import render, redirect
from django.contrib.auth.views import LoginView
from django.contrib.auth.decorators import login_required

from .models import Pokemon, Trainer
from .forms import PokemonForm



def index(request):
    pokemons = Pokemon.objects.all()
    trainers = Trainer.objects.all()
    template = loader.get_template('index.html')
    context = {
        'pokemons': pokemons,
        'trainers': trainers
    }
    return HttpResponse(template.render(context, request))


def pokemon(request, pokemon_id):
    pokemon = Pokemon.objects.get(id=pokemon_id)
    template = loader.get_template('display_pokemon.html')
    context = {'pokemon': pokemon}
    return HttpResponse(template.render(context, request))


def trainers_view(request):  
    all_trainers = Trainer.objects.all()
    template = loader.get_template('trainers.html')
    context = {'trainers': all_trainers}
    return HttpResponse(template.render(context, request))


def trainer(request, trainer_id):
    trainer = Trainer.objects.get(id=trainer_id)
    template = loader.get_template('display_trainer.html')
    context = {'trainer': trainer}
    return HttpResponse(template.render(context, request))

@login_required
def add_pokemon(request):
    if request.method == "POST":
        form = PokemonForm(request.POST, request.FILES)
        if form.is_valid():
            form.save()
            return redirect("pokedex:index")
    else:
        form = PokemonForm()
    return render(request, "pokemon_form.html", {"form": form})
@login_required
def edit_pokemon(request, pokemon_id):
    pokemon = Pokemon.objects.get(id=pokemon_id)
    if request.method == "POST":
        form = PokemonForm(request.POST, request.FILES, instance=pokemon)
        if form.is_valid():
            form.save()
            return redirect("pokedex:index")
    else:
        form = PokemonForm(instance=pokemon)
    return render(request, "pokemon_form.html", {"form": form})

@login_required
def delete_pokemon(request, pokemon_id):
    pokemon = Pokemon.objects.get(id=pokemon_id)
    pokemon.delete()
    return redirect("pokedex:index")


class CustomLoginView(LoginView):
    template_name = 'login.html'
   

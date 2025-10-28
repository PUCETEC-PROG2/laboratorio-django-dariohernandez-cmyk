from django.db import models

class Pokemon(models.Model):
    name = models.CharField(max_length=30, null=False)
    type = models.CharField(max_length=30, null=False)
    weight = models.DecimalField(max_digits=6, decimal_places=4)
    height = models.DecimalField(max_digits=6, decimal_places=4)

    def __str__(self):
        return self.name
    
class Entrenador(models.Model):
    nombre = models.CharField(max_length=50, null=False)
    apellido = models.CharField(max_length=50, null=False)
    nivel = models.CharField(max_length=30, null=False)
    fecha_nacimiento = models.DateField(null=False)

    def __str__(self):
        return f"{self.nombre} {self.apellido}"   
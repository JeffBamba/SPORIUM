# ISTRUZIONI MANUALI - LED BUTTONS (BLK-02.07)

## Modifiche Implementate

Sono stati implementati due pulsanti separati per i LED (Blue ON/OFF e Red ON/OFF) e il collegamento alle luci Unity nella scena.

## Operazioni Manuali Richieste in Unity

### 1. Aggiungere il Componente LedLightController ai Vasi

Per ogni vaso (PotSlot) nella scena:

1. Seleziona il GameObject del vaso nella Hierarchy
2. Nel Inspector, clicca su "Add Component"
3. Cerca e aggiungi il componente `LedLightController`

### 2. Creare le Luci Unity per ogni Vaso

Per ogni vaso, devi creare due luci Unity 2D (una blu e una rossa):

#### Opzione A: Creare le luci come figli del vaso (consigliato)

1. Seleziona il GameObject del vaso
2. Clicca destro → Create → Light → 2D (Point Light)
3. Rinomina il GameObject in "BlueLight"
4. Nel componente Light2D:
   - Imposta il colore a blu (es. RGB: 0, 0.5, 1)
   - Imposta l'intensità a un valore appropriato (es. 1-2)
   - Posiziona la luce sopra il vaso
5. Ripeti per creare "RedLight":
   - Clicca destro sul vaso → Create → Light → 2D (Point Light)
   - Rinomina in "RedLight"
   - Imposta il colore a rosso (es. RGB: 1, 0.2, 0.2)
   - Imposta l'intensità a un valore appropriato (es. 1-2)
   - Posiziona la luce sopra il vaso

#### Opzione B: Assegnare luci esistenti

Se hai già luci nella scena:

1. Seleziona il vaso
2. Nel componente `LedLightController`, trascina la luce blu nel campo "Blue Light"
3. Trascina la luce rossa nel campo "Red Light"

### 3. Configurare i Pulsanti UI

Nel `PotDetailsWidget`:

1. Seleziona il GameObject che contiene il componente `PotDetailsWidget`
2. Nel componente `PotDetailsWidget`, verifica che:
   - `_blueLedButton` sia assegnato al pulsante "LED Blue"
   - `_redLedButton` sia assegnato al pulsante "LED Red"
3. Se i pulsanti non esistono ancora:
   - Crea due Button nella UI
   - Assegna i riferimenti nel componente `PotDetailsWidget`

### 4. Testare il Sistema

1. Avvia il gioco in Play Mode
2. Seleziona un vaso
3. Clicca sul pulsante "LED Blue ON/OFF":
   - La prima volta dovrebbe accendere il LED Blu e la luce blu Unity
   - La seconda volta dovrebbe spegnere il LED Blu e la luce blu Unity
4. Clicca sul pulsante "LED Red ON/OFF":
   - La prima volta dovrebbe accendere il LED Rosso e la luce rossa Unity
   - La seconda volta dovrebbe spegnere il LED Rosso e la luce rossa Unity
5. Verifica che:
   - Quando accendi Blue, Red si spegne automaticamente
   - Quando accendi Red, Blue si spegne automaticamente
   - Quando entrambi sono spenti, nessuna luce è attiva

## Note Importanti

- Le luci Unity vengono controllate automaticamente dal componente `LedLightController`
- Il componente cerca automaticamente le luci con nome "BlueLight" e "RedLight" come figli del vaso
- Se le luci non vengono trovate automaticamente, puoi assegnarle manualmente nel componente
- Le luci vengono inizializzate come spente all'avvio
- Le luci si aggiornano automaticamente quando cambi lo stato del LED tramite i pulsanti

## Troubleshooting

### Le luci non si accendono
- Verifica che il componente `LedLightController` sia presente sul vaso
- Verifica che le luci siano assegnate correttamente nel componente
- Controlla la Console per eventuali errori o warning

### I pulsanti non funzionano
- Verifica che i riferimenti `_blueLedButton` e `_redLedButton` siano assegnati nel `PotDetailsWidget`
- Verifica che i pulsanti siano attivi nella scena

### Le luci non sono visibili
- Verifica che le luci siano attive (GameObject attivo)
- Verifica che il componente Light2D sia abilitato
- Controlla l'intensità e il colore delle luci
- Verifica che le luci siano posizionate correttamente sopra i vasi


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
// todo:
// work on time of day modifiers
// check hunger levels through day
//
// tier 2
// assume health for now
// security needs filled by 
// home need
// employment need
// 
// develop family relationships, extend needs to family
namespace PsychSim
{
    class Program
    {
        // defines the object in the game and runs the game loop
        static void Main(string[] args)
        {
            bool game_on = true;

            


            // generate the world
            World locale = new World(100, 100);
            gametime world_clock = new gametime();
            Console.WriteLine("new clock: " + world_clock.current_time);

            // populate teh world with our test person
            Person adam=spawnAdam(locale,world_clock);            

            // add him to the world
            locale.inhabitants.Add(adam);

            // the game loop
            while (game_on)
            {
                world_clock.tick(locale.inhabitants);
            }
        } // end Main

        // ---------------------------------------------------------------
        // Main Functions
        // ---------------------------------------------------------------
        
        // Adam is a test person. This spawns him and adds him to the world'
        static Person spawnAdam(World locale,gametime world_clock)
        {
            // the test person. Our Adam.
            Person adam = new Person("Adam", new Point(100, 100), locale, world_clock);
            Console.WriteLine("Adam spawned at: " + adam.world_clock.current_time);
            // give him some food
            Item mana = new Item("mana", 1000);
            adam.inventory.Add(mana);
            return adam;
        }

        

        static public string generateID()
        {
            return "0";
        }
    } // end class Program

    //==========================================================================================
    // Other Classes
    //==========================================================================================

    public class gametime
    {
        public DateTime current_time;
        public DateTime last_hour;

        // controls the rate of real world speed
        // 1.0 = 1 tick per real second
        public double speed;

        // controls the in game time ratio
        // 1.0 = 1 game-hour per tick
        public double time_rate;

        public gametime()
        {
            current_time = new DateTime(1900, 1, 1, 6, 0, 0);
            last_hour = new DateTime(1900, 1, 1, 5, 0, 0);
            speed = 0.5;
            time_rate = 0.5;
        }

        // every "tick" or iteration of the game loop, the objects do whatever is defined that they 
        // should do in their onTick method.
        public void tick(List<Person> inhabitants)
        {
            current_time = current_time.AddHours(time_rate);
            Console.WriteLine("Time: " + current_time);
            foreach (Person individual in inhabitants)
            {
                individual.onTick();
            }

            Console.WriteLine("current hour " + current_time.Hour + " last hour: " + last_hour.Hour);
            if (current_time.Hour > last_hour.Hour)
            {
                //Console.WriteLine("check " + (double)(current_time.Hour - last_hour.Hour));
                hourlyTick(inhabitants);
                last_hour=last_hour.AddHours((double)(current_time.Hour - last_hour.Hour));
            }
            Thread.Sleep((int)(1000 * speed));
        }

        // every hour
        public void hourlyTick(List<Person> inhabitants)
        {
            Console.WriteLine("hourly");
            foreach (Person individual in inhabitants)
            {
                individual.onHour();
            }
        }
    }

    // Items are small objects. They can fit in an inventory, be produced, be an ingredient in
    // a production, be used, used up, consumed, owned, sold, stolen, etc.
    public class Item
    {
        public string id;
        public string name;
        public int nutrient; // the nutritional value of the item when it is eaten

        public Item()
        {
            id = "";
        }
        public Item(string na, int nut)
        {
            id = "0"; // need to generate unique ids.
            name = na;
            nutrient = nut;
        }

        // defines what happens when the item is "used"
        // items can be used in different ways. Food, for example can be eaten or stolen
        // pass the way it is used as the use_type
        // only life forms of animal type or their decendnats can use items
        // returns 0 if action successful, 1 if failed.
        public int usage(string use_type, LifeForm user)
        {
            int ret = 0;
            switch (use_type)
            {
                case "eat":
                    user.nutrition += nutrient;
                    if (user.nutrition > user.max_sation)
                        user.nutrition = user.max_sation;

                    //Console.WriteLine("Life form has eaten " + name);
                    break;
                default:
                    //Console.WriteLine("Invalid use type.");
                    ret = 1;
                    break;
            }
            return ret;
        }

        internal bool isNull()
        {
            return (id == "");
        }
    }

    public class Actionable
    {
        public int time_cost;
        public int effort;

        // function type for holding the method which defines the action when it is needed.
        // actor is the id of the object performing the action, from who's action list this was pulled.
        // actee is the item/object the action is performed on
        // should return false if the action was unsucessful, or true if successful
        public delegate bool ActionCall(string actor, string actee);
        // private method to perform the action:
        private ActionCall toDo;

        // the parameters for the action
        string actor_id;
        string actee_id;

        public Actionable(int tim, int eff, ActionCall td, string or,string ee)
        {
            time_cost = tim;
            effort = eff;
            toDo = td;
            actor_id = or;
            actee_id = ee;
        }

        public bool doAction()
        {
            --time_cost;
            return toDo(actor_id, actee_id);
        }
    }

    // the rest data is an encapsulation of all the data relating to getting sleep
    // Not all life forms need to track rest, just animals and their decendants
    public class RestData
    {
        public string rest_state;   // the rest state determined by restfulness
        public int restfulness;     // the current state of restfulness
        public int max_rest;    // the maximum rest possible
        public int rest_drain;  // the speed at which one loses rest per hour
        public double over_rested_level;// above this percent of max level is that too much sleep state
        public double well_rested_level; // above this percent of 
        public double rested_level; // above this percent of max rest is rested
        public double fatigued_level;   // above this percent of max rest is a light fatigue
        public double sleepy_level;     // above this percent of max rest is someone ready for bed
        public double tired_level;      // above this percent of max rest is someone who would pass out at their job
        public double exhausted_level; // above this percent of max rest is someone who would pass out if not actively kept awake
        private Animal owner;   // a reference to the owner of this data

        // number of ticks until rest improves one to the next level: 
        // eg, if 4, and there are 2 ticks to an hour, then every 2 hours of rest, 
        // the sleeper's rest state increases by one level.
        public int rest_rate;   

        // once at zero is death from exhaustion
        public int sleep_time;
        // calculated levels:
        public int over;
        public int well;
        public int rested;
        public int fatigued;
        public int sleepy;
        public int tired;

        public bool nocturnal;
        public World locale;
        public gametime world_clock;

        // establishing the initial rest data
        public RestData(Animal o, World w, gametime clock)
        {
            owner = o;
            max_rest=48;
            rest_drain = 1;
            over_rested_level=0.9;
            well_rested_level=0.7; 
            rested_level=0.6; 
            fatigued_level=0.5;   
            sleepy_level=0.4;     
            tired_level=0.3;
            restfulness = (int)(max_rest * (rested_level + ((well_rested_level - rested_level) / 2)));
            
            sleep_time = 0;
            rest_rate = 4;
            nocturnal = false;
            locale = w;
            world_clock = clock; 
            calcRestState();
            //Console.WriteLine(owner+" rest data received time: " + world_clock.current_time);
        }

        public void onTick()
        {
            if (owner.state == "dead")
                return;
        }

        public void onHour()
        {
            if (owner.state == "dead")
                return;

            if (owner.state != "asleep")
            {
                Console.WriteLine("awake");
                restfulness -= rest_drain;
                calcRestState();
            }
            else
            {
                doSleeping();
            }
        }

        public void doSleeping()
        {
            if (owner.state == "dead")
                return;

            if (restfulness > rested)
            {
                owner.state = "alive";
            }
            else
            {
                owner.state = "asleep";
            }
            Console.WriteLine(owner.state);

            if (owner.state != "asleep")
                return;
            
            if (sleep_time == rest_rate)
            {
                sleep_time = 0;

                //Console.WriteLine("debug: " + rest_state+restfulness);
                switch (rest_state)
                {
                    case "exhausted":
                        restfulness = tired+1;
                        break;
                    case "tired":
                        restfulness = sleepy+1;
                        break;
                    case "sleepy":
                        restfulness = fatigued+1;
                        break;
                    case "fatigued":
                        restfulness = rested+1;
                        break;
                    case "rested":
                        restfulness = well+1;
                        break;
                    case "well rested":
                        restfulness = over+1;
                        break;
                    default:
                        break;
                }
            }
            ++sleep_time;
        }

        public void calcRestState()
        {
            over=(int)( max_rest * over_rested_level);
            well=(int)( max_rest * well_rested_level);
            rested = (int)(max_rest * rested_level);
            fatigued = (int)(max_rest * fatigued_level);
            sleepy = (int)(max_rest * sleepy_level);
            tired = (int)(max_rest * tired_level);

            int modrest=cycleModifiedRestfulness();
            if (modrest > over)
                rest_state = "over rested";
            else if (modrest <= over && modrest > well)
                rest_state = "well rested";
            else if (modrest <= well && modrest > rested)
                rest_state = "rested";
            else if (modrest <= rested && modrest > fatigued)
                rest_state = "fatigued";
            else if (modrest <= fatigued && modrest > sleepy)
                rest_state = "sleepy";
            else if (modrest <= sleepy && modrest > tired)
                rest_state = "tired";
            else if (modrest <= tired && modrest > 0)
                rest_state = "exhausted";
            else if (restfulness <= 0 && modrest <= 0)
            {
                owner.state = "dead";
                Console.WriteLine(owner + " died of exhaustion at modrest: "+modrest+" and restfulness: "+restfulness);
            }
        }

        // sleep cycles are determined by diurnal/noctournal animal type and the time of day
        public int cycleModifiedRestfulness()
        {
            int newrest=restfulness;
            
            int start_hour = (nocturnal) ? 18 : 6;
            if (world_clock.current_time.Hour >= start_hour - 6 && world_clock.current_time.Hour <= start_hour + 6)
            {
                newrest += 4*(world_clock.current_time.Hour - start_hour);
            }
            else
            {
                newrest += 4*(24 - start_hour - world_clock.current_time.Hour);
            }
            
            Console.WriteLine("restful=" + restfulness + " feels like=" + newrest);
            return newrest;
        }
    }

    // the base class of all life. Will have those properties that will be common to all life forms in the game.
    // not much at the moment. All it does id starve to death.
    public class LifeForm
    {
        public string id;
        // current states are "alive" or "dead"
        public string state;
        public int nutrition;   // life forms all need nutrition to live. when it reaches 0, they die
        public bool hungry;         // is or isnt hungry
        public double hunger_level; // percentage of max_sation it takes to be hungry
        public int max_sation;      // the highet level of nutrients that can be processed by a life form, period.
        public int burn_speed;      // number of nutrients burned per hour
        public Point location;
        public World locale;
        public Queue<Actionable> action_queue;
        public List<Item> inventory;
        public gametime world_clock;
        
        

        // When a life form is created, we can chose its starting nutrition
        // and usualy, they start alive.
        public LifeForm(Point start_location, World w,gametime clock)
        {
            id = Program.generateID(); // need to develop a global system to determine IDs and reference objects by their IDs
            
            state = "alive";
            hungry = false;
            hunger_level = 0.5;
            max_sation = 96;
            burn_speed = 1;
            nutrition = 75;
            location = start_location;
            locale = w;
            action_queue=new Queue<Actionable>();
            inventory= new List<Item>();
            world_clock = clock;

            // debug feedback
            //Console.WriteLine("Lifeform created at coordinates "+location);
            //Console.Write("Nutrition: " + nutrition +"\n");

        }

        // define a null LIfeForm
        public LifeForm()
        {
            id="";
        }

        // informs if LifeForm is null or not
        public bool isNull()
        {
            return (id == "");
        }

        // every tick is a game turn. This function defines what happens to the life form every turn
        public void onTick()
        {

            Console.WriteLine("state: " + state);
            // check pre states
            if (state == "dead")
                return;

            performNextAct();

            //set states
            if (nutrition <= 0)
            {
                state = "dead";
                Console.WriteLine("Lifeform has starved.");
            }

            if (nutrition <= hunger_level*max_sation)
                hungry = true;
            else
                hungry = false;

            // check post states
            if (state == "dead")
                return;

            if (hungry)
            {
                Console.WriteLine("Lifeform is hungry.");
            }
        }

        public void onHour()
        {
            if (state == "dead")
                return;

            if (state == "alive")
                nutrition -= burn_speed;
        }

        public void performNextAct()
        {
            if (action_queue.Count != 0)
            {
                Actionable next_action = action_queue.Dequeue();
                next_action.doAction();
            }
        }

        // adds an action to the action queue.
        // exists as its own method for the potential addition of
        // priority queue implementation in the future.
        public void addAction(Actionable act)
        {
            action_queue.Enqueue(act);
        }

        public bool actionEat(string eater_id, string item_id)
        {
            LifeForm eater = new LifeForm();
            Item eaten_item = new Item();
            foreach (LifeForm entity in locale.inhabitants)
            {
                if (entity.id == eater_id)
                {
                    eater = entity;
                    break;
                }
            }
            if (eater.isNull())
                return false;

            foreach (Item food in eater.inventory)
            {
                if (food.id == item_id)
                {
                    eaten_item = food;
                    break;
                }
            }
            if (eaten_item.isNull())
                return false;

            eaten_item.usage("eat", eater);
            eater.inventory.Remove(eaten_item);
            eater.hungry = false;
            return true;
        }
    }

    // animals are a type of Life Form. They arent very distinct at the moment
    // animals can have names
    public class Animal:LifeForm
    {
        public string name;
        public RestData rest_data;
        public int safety;

        public Animal(string nm, Point start_location, World w, gametime clock)
            : base(start_location, w, clock)
        {
            rest_data = new RestData(this,w,clock);
            name = nm;
            safety = 100;
        }

        new public void onTick()
        {
            //Console.WriteLine("animal ontick: ");
            base.onTick();
            rest_data.onTick();
        }

        new public void onHour()
        {
            base.onHour();
            rest_data.onHour();
            Console.WriteLine(rest_data.restfulness);
        }
    }

    // people will be the main focus of the game, though they are little more than animals at the moment
    // a person is considered a modern person. It will expect to have contemporary resources available to it.
    // A person differs from an animal in that it can have an inventory filled with items.
    // Physiological needs, breathing, water, and excrement are going to be taken for granted
    public class Person : Animal
    {

        public Person(string nm, Point start_location, World w, gametime clock)
            : base(nm, start_location, w, clock)
        {
            //rest_data = new RestData(this, w, clock);
            inventory = new List<Item>();
            Console.WriteLine("Initiating a person: " + world_clock.current_time);
            
        }

        // performs all onTick of LifeForm and adds Person reaction to needs
        new public void onTick()
        {
            
            base.onTick();

            if (state == "dead")
                return;

            // plan actions
            if (hungry)
            {
                obtainFood();
            }

            if (rest_data.restfulness < 50 && safety > 50)
            {
                rest_data.doSleeping();
                //Console.WriteLine("ding");
            }

            Console.WriteLine(rest_data.rest_state);

        }

        new public void onHour()
        {
            base.onHour();
        }

        // When hungry, a Person will follow a particular series of steps to eat
        // Making different assumptions than other Animals
        public void obtainFood()
        {
            bool found_food = false;
            int radius = 5;
            Rectangle d_region = new Rectangle(location.X-radius,location.Y-radius, (2*radius),(2*radius));

            // search inventory for food_item
            foreach (Item inv_item in inventory)
            {
                if (inv_item.nutrient > 1)
                {
                    found_food = true;
                    addAction(new Actionable(1,1,actionEat,this.id,inv_item.id));
                    
                    break;
                }
            }

            // no food in inventory, locate food source
            if (!found_food)
            {
                Lot food_source = typeRadialSearch(locale, location, radius, "food");
                if (!food_source.isNull())
                {
                    //Console.WriteLine(this.name + " has found food at "+food_source.coordinates);
                }

                locale.addDemand(new Demand(locale, "food", d_region, id));
                //Console.WriteLine(this.name + " can not find food!.");
            }
        }

        // search a given area in the World and find the closest lot with the building type on it
        // returns the lot.
        // * this might be turned into a template later on for different kinds of area searches
        public Lot typeRadialSearch(World locale, Point center, int radius, string type)
        {
            Point p=new Point(); // the search pointer
            for (p.X = center.X; (p.X < center.X + radius); ++p.X)
            {
                for (p.Y = center.Y; p.Y < center.Y + radius; ++p.Y)
                {
                    // skip checking places out of bounds
                    if (
                        p.X<0 ||
                        p.X> locale.world_size.X-1 ||
                        p.Y<0 ||
                        p.Y>locale.world_size.Y-1
                        )
                        continue;

                    // convert the 2d search into a 1d index lookup
                    int index = (p.Y * locale.world_size.X) + p.X;
                    if (locale.lot_list[index].building.type == type)
                        return locale.lot_list[index];
                }
            }
            
            // failed search, return the null lot:
            return new Lot();
        }

    } // end class Person

    // a lot is a tile in the world. It has a fixed location. It can have a building constructed on it.
    // LifeForms and Items and other objects with the same lot coordiantes exist on this lot location
    public class Lot
    {
        public Point coordinates;
        public Building building;
        public string zoning;
        public double value;

        // declare and define constructor
        public Lot(int x,int y,Building bld,string zone,double val)
        {
            coordinates.X = x;
            coordinates.Y = y;
            building = bld;
            zoning = zone;
            value = val;
        }

        // a null lot
        public Lot()
        {
            coordinates.X = -1;
            coordinates.Y = -1;
            building = new Building();
            zoning = "";
            value = -1;
        }

        //returns true if the lot is "null" or does not really exist in world
        public bool isNull()
        {
            return (coordinates.X == -1);
        }
    }

    // Buildings are built on lots
    public class Building
    {
        public string type;
        public double value;
        public string name;

        // basic constructor for a null building, which would be found on a vacant lot
        public Building()
        {
            type = "";
            value = 0.0;
            name = "";
        }
    }

    // the world is where the game takes place. Intantiated so that travel is possible from one "world" to anoter (travel)
    public class World
    {
        public Point world_size;
        public List<Lot> lot_list;
        // all living things are in the world when they are in this list
        public List<Person> inhabitants;
        public List<Demand> suggestion_box;
        //public gametime timer;
        

        public World(int x, int y)
        {
            //timer = new gametime();
            //Console.WriteLine("world time " + timer.world_clock);
            lot_list = new List<Lot>();
            inhabitants = new List<Person>();
            suggestion_box = new List<Demand>();

            // set the world size
            world_size.X = x;
            world_size.Y = y;
            //fill the world with lots of lots.
            for (int a = 0; a < world_size.X; ++a)
            {
                for (int b = 0; b < world_size.Y; ++b)
                {
                    lot_list.Add(new Lot(a, b, new Building(), "unzoned", 1000.00));
                }
            }
            //Console.WriteLine("World created: " + world_size);
        }

        // adds the demand to the world sugestion box, but doesnt allow any demand 
        // of the same type 
        // from the same petitioner
        // in the same area
        public void addDemand(Demand temp)
        {
            foreach (Demand suggestion in suggestion_box)
            {
                if (suggestion.origin == temp.origin &&
                    suggestion.type == temp.type &&
                    suggestion.area.IntersectsWith(temp.area)
                    )
                    return;
            }

            suggestion_box.Add(temp);
            //Console.WriteLine(temp.origin + " demands " + temp.type + " at " + temp.area);
        }
    }// end class World

    // a demand is a request ticket. If a person requires a resource, and it is not available
    // within the constraints that it is wanted, a demand is added to the world's deman list.
    // the list can then be used to determine societal growth, based on popular demand.
    public class Demand
    {
        public string type;     // the type of demand made
        public Rectangle area;  // general area the demand is desired
        public string origin;   // the person making the demand's ID

        public Demand(World w, string t, Rectangle a, string p)
        {
            type = t;
            origin = p;
            area = Rectangle.Intersect(a,new Rectangle(0, 0, w.world_size.X, w.world_size.Y));
            if (area.IsEmpty)
            {
                //Console.WriteLine("rectangle out of bounds error.");
            }
        }
    }

} // end namespace PsychSim

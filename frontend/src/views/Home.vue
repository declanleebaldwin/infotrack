<template>
  <div class="home">
    <section class="hero is-primary">
      <div class="hero-body">
        <div class="container">
          <h1 class="title">
            InfoTrack Development Test
          </h1>
          <h2 class="subtitle">
            Declan Baldwin
          </h2>
        </div>
      </div>
    </section>
    <section>
      <div class="container">
        <div class="columns">
          <div class="column is-half">
            <div class="container">
              <h5 class="title mt is-5">Please fill in the form to find out where a URL ranks in Google.</h5>
              <div class="field">
                <label class="label">Search Terms</label>
                <div class="control">
                  <input class="input" :class="{'is-danger': error.search}" v-model="search" type="text" placeholder="e.g land registry searches" @input="validateSearch" @blur="validateSearch"/>
                </div>
                <p class="help is-danger" v-show="error.search">Please enter a search term</p>
              </div>
              <div class="field">
                <label class="label">URL</label>
                <div class="control">
                  <input class="input" :class="{'is-danger': error.url}" v-model="url" type="text" placeholder="e.g. www.infotrack.co.uk" @input="validateURL" @blur="validateURL"/>
                </div>
                <p class="help is-danger" v-show="error.url">Please enter a URL</p>
              </div>
              <div class="control">
                <button v-show="!loading" @mousedown="validateForm" class="button is-primary">Submit</button>
                <div v-show="loading" class="loader"></div>
              </div>
              <h5 v-show="this.googleRank.length > 0 && showResult" class="title mt is-5">Your URL was ranked {{ googleRankString }} in Google's top 100 results.</h5>
              <h5 v-show="this.googleRank.length == 0  && showResult" class="title mt is-5">Your URL was not ranked in Google's top 100 results.</h5>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script>
import axios from 'axios'
export default {
  name: 'home',
  data () {
    return {
      loading: false,
      search: '',
      url: '',
      googleRank: [],
      showResult: false,
      error: {
        search: false,
        url: false
      }
    }
  },
  methods: {
    validateForm () {
      this.validateSearch()
      this.validateURL()
      if (this.error.search || this.error.url) return
      this.submitForm()
    },
    validateSearch () {
      if (this.search === '') {
        this.error.search = true
      } else {
        this.error.search = false
      }
    },
    validateURL () {
      if (this.url === '') {
        this.error.url = true
      } else {
        this.error.url = false
      }
    },
    async submitForm () {
      let $this = this
      try {
        this.showResult = false
        this.loading = true
        const response = await axios.post('https://localhost:44393/api/ranks', {
          SearchTerm: $this.search,
          URL: $this.url
        })
        $this.loading = false
        this.showResult = true
        $this.googleRank = response.data
      } catch (error) {
        $this.loading = false
        console.error(error)
      }
    }
  },
  computed: {
    googleRankString () {
      return this.googleRank.join(', ')
    }
  }
}
</script>

<style lang="css" scoped>
.loader {
  border: 8px solid #f3f3f3; /* Light grey */
  border-top: 8px solid #3498db; /* Blue */
  border-radius: 50%;
  width: 50px;
  height: 50px;
  animation: spin 2s linear infinite;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

.mt {
  margin-top: 25px;
}
</style>
